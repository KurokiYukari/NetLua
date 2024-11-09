using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NetLua
{
    /// <summary>
    /// see https://www.lua.org/manual/5.4/manual.html#6.1
    /// </summary>
    public class BasicLibrary : ILuaLibrary
    {
        public const string _VERSION = "Lua 5.4";

        public static BasicLibrary Instance { get; } = new BasicLibrary();

        public void AddLibrary(LuaContext context)
        {
            context.Set("assert", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                return Assert(args[0], args[1]);
            }));
            context.Set("collectgarbage", LuaObject.FromFunction(args =>
            {
                CollectGarbage();
                return Lua.Return();
            }));
            context.Set("dofile", LuaObject.FromFunction(args =>
            {
                return DoFile(context, args[0]);
            }));
            context.Set("error", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                int? level = args[1].AsInt();
                Error(args[0], level);
                return Lua.Return();
            }));
            context.Set("getmetatable", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                return Lua.Return(GetMetaTable(args[0]));
            }));
            context.Set("ipairs", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                var (iterator, t, index) = IPairs(args[0]);
                return Lua.Return(iterator, t, index);
            }));
            context.Set("load", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                // TODO: support function chunk arg
                var chunk = args[0].ToString();
                var function = Load(context, chunk, args[1].AsString(), args[2].AsString(), args[3]);
                return Lua.Return(function);
            }));
            context.Set("loadfile", LuaObject.FromFunction(args =>
            {
                var function = LoadFile(context, args[0], args[1], args[2]);
                return Lua.Return(function);
            }));
            context.Set("next", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                var result = Next(args[0], args[1]);
                if (result == null)
                {
                    return Lua.Return(LuaObject.Nil);
                }
                return Lua.Return(result.Value.index, result.Value.value);
            }));
            context.Set("pairs", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                var (next, t, index) = Pairs(context, args[0]);
                return Lua.Return(next, t, index);
            }));
            context.Set("pcall", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                var (succeed, funcCallResult) = PCall(args[0], args.Skip(1).ToArray());
                var result = Lua.Return(succeed);
                result = result.Concat(funcCallResult);
                return result;
            }));
            context.Set("print", LuaObject.FromFunction(args =>
            {
                Print(args);
                return Lua.Return();
            }));
            context.Set("rawequal", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 2);
                return Lua.Return(RawEqual(args[0], args[1]));
            }));
            context.Set("rawget", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 2);
                return Lua.Return(RawGet(args[0], args[1]));
            }));
            context.Set("rawlen", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                return Lua.Return(RawLen(args[0]));
            }));
            context.Set("rawset", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 3);
                return Lua.Return(RawSet(args[0], args[1], args[2]));
            }));
            context.Set("select", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                return Select(args[0], args.Skip(1).ToArray());
            }));
            context.Set("setmetatable", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 2);
                return Lua.Return(SetMetaTable(args[0], args[1]));
            }));
            context.Set("tonumber", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                long? @base = null;
                if (args.Length > 1)
                {
                    var baseArg = args[1];
                    if (baseArg != null)
                    {
                        if (baseArg.TryConvertToInt(out var temp))
                        {
                            @base = temp;
                        }
                        else
                        {
                            GuardLibrary.ArgumentError(2, "tonumber", GuardLibrary.NOT_INT_NUMBER);
                            return Lua.Return();
                        }
                    }
                }

                return Lua.Return(ToNumber(context, args[0], @base));
            }));
            context.Set("tostring", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                return Lua.Return(ToString(args[0]));
            }));
            context.Set("type", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 1);
                return Lua.Return(Type(args[0]).ToString());
            }));
            context.Set(WarnProxy.WARN_NAME, new WarnProxy(context).Warn);
            context.Set("xpcall", LuaObject.FromFunction(args =>
            {
                GuardLibrary.HasLengthAtLeast(args, 2);
                var (succeed, funcCallResult) = XPCall(args[0], args[1], args.Skip(2).ToArray());
                var result = Lua.Return(succeed);
                result = result.Concat(funcCallResult);
                return result;
            }));
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-assert
        /// </summary>
        /// <param name="v"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static LuaArguments Assert(LuaObject v, LuaObject message = null)
        {
            if (v == null || !v.AsBool())
            {
                if (message == null)
                {
                    message = "assertion failed!";
                }
                Error(message);
            }
            return Lua.Return(v, message);
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-collectgarbage
        /// </summary>
        public static void CollectGarbage()
        {
            // TODO: implement CollectGarbage
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        [DoesNotReturn]
        public static void Error(LuaObject message, int? level = null)
        {
            // TODO: support level
            throw new LuaException(message.ToString());
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-dofile
        /// </summary>
        /// <param name="file"></param>
        public static LuaArguments DoFile(LuaContext context, string file = null)
        {
            if (file == null)
            {
                var content = Console.In.ReadToEnd();
                return context.DoString(content, "stdin");
            }
            else
            {
                return context.DoFile(file);
            }
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-getmetatable
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static LuaObject GetMetaTable(LuaObject @object)
        {
            return @object.GetMetaTable();
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-ipairs
        /// </summary>
        /// <returns></returns>
        public static (LuaFunction iterator, LuaObject t, int index) IPairs(LuaObject t)
        {
            return (_iterator, t, 0);
        }

        private static readonly LuaFunction _iterator = args =>
        {
            var t = args[0];
            var index = args[1].AsNumber() + 1;

            var val = t[index];
            if (val == LuaObject.Nil)
                return Lua.Return(LuaObject.Nil);
            else
                return Lua.Return(index, val);
        };

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-load
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="chunkName"></param>
        /// <param name="mode"></param>
        /// <param name="env"></param>
        public static LuaFunction Load(LuaContext context, string chunk, string chunkName = null, string mode = null, LuaObject env = null)
        {
            chunkName ??= "chunk";
            var function = context.Load(chunk, chunkName, env);
            return function;
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-loadfile
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fileName"></param>
        /// <param name="mode"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static LuaFunction LoadFile(LuaContext context, string fileName = null, string mode = null, LuaObject env = null)
        {
            string chunk = null;
            string chunkName = null;
            if (fileName == null)
            {
                chunk = Console.In.ReadToEnd();
                chunkName = "stdin";
            }
            else
            {
                chunk = File.ReadAllText(fileName);
                chunkName = fileName;
            }
            return Load(context, chunk, chunkName, mode, env);
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-next
        /// </summary>
        /// <param name="table"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static (LuaObject index, LuaObject value)? Next(LuaObject table, LuaObject index)
        {
            if (!table.IsTable)
            {
                Error("t must be a table");
            }
            var dict = table.AsTable();
            if (index.IsNil)
            {
                if (dict.Count == 0)
                    return null;
                var next = dict.First();
                return (next.Key, next.Value);
            }
            else
            {
                var next = dict.SkipWhile(pair => pair.Key != index).Skip(1).FirstOrDefault();
                if (next.Key == null && next.Value == null)
                {
                    return null;
                }
                return (next.Key, next.Value);
            }
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-pairs
        /// </summary>
        /// <param name="context"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static (LuaObject next, LuaObject t, LuaObject index) Pairs(LuaContext context, LuaObject t)
        {
            LuaObject handler = t.GetMetaMethod(LuaEvents.PAIRS);
            if (!handler.IsNil)
            {
                var result = handler.Call(Lua.Return(t));
                return (result[0], result[1], result[2]);
            }
            else
            {
                return (context.Get("next"), t, LuaObject.Nil);
            }
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-pcall
        /// </summary>
        /// <param name="func"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public (bool, LuaArguments) PCall(LuaObject func, LuaArguments args)
        {
            try
            {
                var result = func.Call(args);
                return (true, result);
            }
            catch (Exception e)
            {
                return (false, Lua.Return(e.Message));
            }
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-print
        /// </summary>
        /// <param name="args"></param>
        public static void Print(LuaArguments args)
        {
            Print(string.Empty, args);
        }

        public static void Print(string prefix, LuaArguments args)
        {
            var writer = Console.Out;
            writer.Write(prefix);
            foreach (var item in args)
            {
                writer.Write(item.ToString());
            }
            writer.WriteLine();
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-rawequal
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public bool RawEqual(LuaObject v1, LuaObject v2)
        {
            return Equals(v1?.AsUserData(), v2?.AsUserData());
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-rawget
        /// </summary>
        /// <param name="table"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="LuaException"></exception>
        public static LuaObject RawGet(LuaObject table, LuaObject index)
        {
            GuardLibrary.EnsureType(table, 0, LuaType.table, "rawget");
            if (table.AsTable().TryGetValue(index, out var obj))
                return obj;
            else
                return LuaObject.Nil;
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-rawlen
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static int RawLen(LuaObject table)
        {
            GuardLibrary.EnsureType(table, 0, LuaType.table, "rawlen");
            return table.AsTable().Count;
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-rawset
        /// </summary>
        /// <param name="table"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static LuaObject RawSet(LuaObject table, LuaObject index, LuaObject value)
        {
            GuardLibrary.EnsureType(table, 0, LuaType.table, "rawset");
            if (index == null)
            {
                Error("table index is nil");
            }
            table.AsTable()[index] = value;
            return table;
        }

        // TODO: Implement in Modules
        // public static void Require(LuaContext context, string modName)
        // {
        //     var packages = context.Get("package");
        // }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-select
        /// </summary>
        /// <param name="index"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static LuaArguments Select(LuaObject index, LuaArguments args)
        {
            if (index.IsNumber)
            {
                if (index.TryConvertToInt(out long longIndex))
                {
                    var intIndex = (int)longIndex;
                    if (intIndex == 0 || index > args.Length || index < -args.Length)
                    {
                        GuardLibrary.ArgumentError(1, "select", GuardLibrary.INDEX_OUT_OF_RANGE);
                        return Lua.Return();
                    }
                    else if (intIndex > 0)
                    {
                        return Lua.Return(args.Skip(intIndex - 1).ToArray());
                    }
                    else
                    {
                        return Lua.Return(args.Skip(args.Length + intIndex).ToArray());
                    }
                }
                else
                {
                    GuardLibrary.ArgumentError(1, "select", GuardLibrary.NOT_INT_NUMBER);
                    return Lua.Return();
                }
            }
            else if (index.IsString)
            {
                if (index.AsString() == "#")
                {
                    return Lua.Return(args.Length);
                }
                else
                {
                    return Lua.Return();
                }
            }
            else
            {
                GuardLibrary.EnsureType(index, 0, LuaType.number, "select");
                return Lua.Return();
            }
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-setmetatable
        /// </summary>
        /// <param name="table"></param>
        /// <param name="metaTable"></param>
        /// <returns></returns>
        public static LuaObject SetMetaTable(LuaObject table, LuaObject metaTable)
        {
            GuardLibrary.EnsureType(table, 0, LuaType.table, "setmetatable");
            return table.SetMetaTable(metaTable, false);
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-tonumber
        /// </summary>
        /// <param name="context"></param>
        /// <param name="e"></param>
        /// <param name="base"></param>
        /// <returns></returns>
        public static double? ToNumber(LuaContext context, LuaObject e, int? @base = null)
        {
            if (@base == null)
            {
                if (e.IsNumber)
                {
                    return e.AsNumber();
                }
                else if (e.IsString)
                {
                    var result = context.DoString($"return {e.AsString()}")[0];
                    if (result.IsNumber)
                    {
                        return result.AsNumber();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                GuardLibrary.EnsureType(e, 0, LuaType.@string, "tonumber");
                var baseValue = @base.Value;
                if (baseValue >= 2 && baseValue <= 36)
                {
                    if (TryConvertToBaseAlpha(e.AsString(), baseValue, out int result))
                    {
                        return result;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    GuardLibrary.ArgumentError(2, "tonumber", "base out of range");
                    return 0;
                }
            }
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-tostring
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static string ToString(LuaObject v)
        {
            return v.ToString();
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-type
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static LuaType Type(LuaObject v)
        {
            return v.Type;
        }

        class WarnProxy
        {
            public LuaContext Context { get; }
            
            public LuaObject Warn { get; }
            private LuaObject _warnOff;

            public const string WARN_NAME = "warn";

            public WarnProxy(LuaContext context)
            {
                Context = context;

                Warn = LuaObject.FromFunction(args =>
                {
                    GuardLibrary.EnsureType(args, 0, LuaType.@string, WARN_NAME);
                    var str = args[0].ToString();
                    if (str == "@on")
                    {
                        context.Set(WARN_NAME, _warnOff);
                    }
                    return Lua.Return();
                });
                _warnOff = LuaObject.FromFunction(args =>
                {
                    GuardLibrary.EnsureType(args, 0, LuaType.@string, WARN_NAME);
                    var str = args[0].ToString();
                    if (str == "@off")
                    {
                        context.Set(WARN_NAME, Warn);
                        return Lua.Return();
                    }

                    str = string.Join(string.Empty, args.Select(a => a.ToString()));
                    Print("Lua warning: ", args);
                    return Lua.Return();
                });
            }
        }

        /// <summary>
        /// see https://www.lua.org/manual/5.4/manual.html#pdf-xpcall
        /// </summary>
        /// <param name="func"></param>
        /// <param name="msgHandler"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static (bool, LuaArguments) XPCall(LuaObject func, LuaObject msgHandler, LuaArguments args)
        {
            try
            {
                var result = func.Call(args);
                return (true, result);
            }
            catch (Exception e)
            {
                var errorArgs = msgHandler.Call(Lua.Return(e.Message));
                return (false, Lua.Return(errorArgs[0]));
            }
        }

        private static bool TryConvertToBaseAlpha(string alpha, int @base, out int result)
        {
            result = 0;
            int intPower = 1;

            for (int i = alpha.Length - 1; i >= 0 ; i--)
            {
                var c = alpha[i];
                int value = -1;
                if (c >= '0' && c <= '9')
                {
                    value = c - '0';
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    value = c - 'A' + 10;
                }
                else if (c >= 'a' && c <= 'z')
                {
                    value = c - 'a' + 10;
                }

                if (value == -1 || value >= @base)
                {
                    result = 0;
                    return false;
                }

                result += value * intPower;
                intPower *= @base;
            }

            return true;
        }
    }
}