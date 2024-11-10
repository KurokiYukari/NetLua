using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetLua
{
    public class PackageLibrary : ILuaLibrary
    {
        public static PackageLibrary Instance { get; } = new PackageLibrary();

        public void AddLibrary(LuaContext context)
        {
            var instance = new PackageLibraryInstance(context);
            context.Set("require", new LuaFunction(instance.Require));
        }

        class PackageLibraryInstance
        {
            private readonly LuaContext _context;

            public LuaObject PackageTable => _context.GetOrSet("package", LuaObject.NewTable);

            // different from lua official implement
            private static readonly Lazy<string> _defaultPath = new Lazy<string>(() => 
            {
                return Environment.GetEnvironmentVariable($"LUA_PATH_{BasicLibrary._ENV_SUFFIX_VERSION}")
                    ?? Environment.GetEnvironmentVariable("LUA_PATH")
                    ?? @".\?.lua;" +
                       @".\?\init.lua";
            });
            private static readonly Lazy<string> _defaultCPath = new Lazy<string>(() =>
            {
                return Environment.GetEnvironmentVariable($"LUA_CPATH_{BasicLibrary._ENV_SUFFIX_VERSION}")
                    ?? Environment.GetEnvironmentVariable("LUA_CPATH")
                    ?? @".\?.dll";
            });

            private const string CONFIG_PATH_SEP = ";";
            private const string CONFIG_NAME_REP = "?";
            private const string CONFIG_EXE_DIR_REP = "!";
            private static readonly Lazy<string> _defaultConfig = new Lazy<string>(() =>
            {
                var sb = new StringBuilder();
                // directory separator
                sb.Append(Path.PathSeparator);
                sb.AppendLine();
                // path separator
                sb.AppendLine(CONFIG_PATH_SEP);
                // substitution points
                sb.AppendLine(CONFIG_NAME_REP);
                // executable's directory
                sb.AppendLine(CONFIG_EXE_DIR_REP);
                // ignore all text after it when building the luaopen_ function name.
                sb.AppendLine("-");
                return sb.ToString();
            });

            public PackageLibraryInstance(LuaContext context)
            {
                _context = context;
                var package = PackageTable;
                
                package["path"] = _defaultPath.Value;
                package["cpath"] = _defaultCPath.Value;
                package["searchpath"] = LuaObject.FromFunction(SearchPath);
                package["config"] = _defaultConfig.Value;
                package["loadlib"] = LuaObject.FromFunction(LoadLib);

                var searchers = package.GetOrSet("searchers", LuaObject.NewTable);
                TableLibrary.Insert(searchers, LuaObject.FromFunction(Searcher_Preload));
                TableLibrary.Insert(searchers, LuaObject.FromFunction(Searcher_Path));
                // TODO: add searchers for C lib
            }

            public LuaArguments Require(LuaArguments args)
            {
                GuardLibrary.HasLengthAtLeast(args, 1, "require");

                var moduleName = GuardLibrary.EnsureString(args, 0, "require");
                var package = PackageTable;
                var loaded = package.GetOrSet("loaded", LuaObject.NewTable);

                var module = loaded[moduleName];
                if (module.IsNil)
                {
                    var searchers = package["searchers"];
                    if (!searchers.IsTable)
                    {
                        throw new LuaException("'package.searchers' must be a table");
                    }

                    var sb = new StringBuilder();
                    var loader = LuaObject.Nil;
                    var loaderData = LuaObject.Nil;
                    foreach (var (_, v) in searchers)
                    {
                        var searcherResult = v.Call(args);
                        var firstSearcherResult = searcherResult[0];
                        if (firstSearcherResult.IsFunction)
                        {
                            loader = firstSearcherResult;
                            loaderData = searcherResult[1];
                            break;
                        }
                        else if (!firstSearcherResult.IsNil)
                        {
                            using var reader = new StringReader(firstSearcherResult.ToString());
                            string line = null;
                            while ((line = reader.ReadLine()) != null)
                            {
                                sb.Append('\t');
                                sb.AppendLine(line);
                            }
                        }
                    }
                    if (loader.IsNil)
                    {
                        throw new LuaException($"module '{moduleName}' not found:{Environment.NewLine}{sb}");
                    }

                    var loaderResult = loader.Call(loaderData);
                    module = loaderResult[0];
                    if (!module.IsNil)
                    {
                        return Lua.Return(module, loaderData);
                    }
                    else
                    {
                        module = loaded.GetOrSet(moduleName, () => LuaObject.True);
                    }
                }
                return Lua.Return(module);
            }

            public (string file, string err) SearchPath(string name, string path, string sep = null, string rep = null)
            {
                sep ??= ".";
                rep ??= Environment.NewLine;

                name = name.Replace(sep, rep);
                path = path.Replace(CONFIG_NAME_REP, name);
                path = path.Replace(CONFIG_EXE_DIR_REP, AppDomain.CurrentDomain.BaseDirectory);
                var splits = path.Split(CONFIG_PATH_SEP);
                foreach (var file in splits)
                {
                    var fullPath = _context.GetFullPath(file, false);
                    if (File.Exists(fullPath))
                    {
                        return (file, null);
                    }
                }

                return (null, string.Join(Environment.NewLine, splits.Select(p => $"no file '{p}'")));
            }

            public LuaArguments SearchPath(LuaArguments args)
            {
                const string F_NAME = "searchpath";
                var name = GuardLibrary.EnsureString(args, 0, F_NAME);
                var path = GuardLibrary.EnsureString(args, 1, F_NAME);
                string sep = null;
                if (args.Length >= 3)
                {
                    sep = GuardLibrary.EnsureString(args, 2, F_NAME);
                }
                string rep = null;
                if (args.Length >= 4)
                {
                    rep = GuardLibrary.EnsureString(args, 3, F_NAME);
                }
                var (file, err) = SearchPath(name, path, sep, rep);
                if (err == null)
                {
                    return Lua.Return(file);
                }
                else
                {
                    return Lua.Return(file, err);
                }
            }

            public LuaArguments Searcher_Preload(LuaArguments args)
            {
                var moduleName = args[0];
                var package = PackageTable;
                var preload = package["preload"];
                if (!preload.IsNil)
                {
                    var loader = preload[moduleName];
                    if (!loader.IsNil)
                    {
                        return Lua.Return(loader, ":preload:");
                    }
                }

                return Lua.Return($"no field package.preload['{moduleName}']");
            }

            public LuaArguments Searcher_Path(LuaArguments args)
            {
                var moduleName = args[0];
                var package = PackageTable;
                var path = package["path"];
                if (!path.IsString)
                {
                    throw new LuaException("'package.path' must be a string");
                }

                var (file, err) = SearchPath(moduleName, path, null, null);
                if (file == null || err != null)
                {
                    return Lua.Return(err);
                }

                return Lua.Return(LuaObject.FromFunction((args) =>
                {
                    var path = GuardLibrary.EnsureString(args, 0, "dofile");
                    return _context.DoFile(path);
                }), file);
            }

            public LuaArguments LoadLib(LuaArguments args)
            {
                throw new NotImplementedException("'loadlib' is not supported for NetLua");
            }
        }
    }
}
