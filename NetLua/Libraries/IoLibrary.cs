using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace NetLua
{
    public class IoLibrary : ILuaLibrary
    {
        readonly LuaObject _fileMetaTable = LuaObject.NewTable();

        public IoLibrary()
        {
            var __index = LuaObject.NewTable();
            _fileMetaTable["__index"] = __index;

            __index["close"] = new LuaFunction(FileObject.Close);
            __index["flush"] = new LuaFunction(FileObject.Flush);
            __index["lines"] = new LuaFunction(FileObject.Lines);
            __index["read"] = new LuaFunction(FileObject.Read);
            __index["write"] = new LuaFunction(FileObject.Write);
            __index["seek"] = new LuaFunction(FileObject.Seek);
        }

        public void AddLibrary(LuaContext context)
        {
            var io = LuaObject.NewTable();

            var process = Process.GetCurrentProcess();
            var stdin = LuaObject.FromObject(new FileObject(null, process.StandardInput));
            stdin.SetMetaTable(_fileMetaTable, true);
            io["stdin"] = stdin;
            var stdout = LuaObject.FromObject(new FileObject(process.StandardOutput, null));
            stdout.SetMetaTable(_fileMetaTable, true);
            io["stdout"] = stdout;
            var stderr = LuaObject.FromObject(new FileObject(process.StandardError, null));
            stderr.SetMetaTable(_fileMetaTable, true);
            io["stderr"] = stderr;

            var currentInput = stdin;
            var currentOutput = stdout;

            var outputFunc = new LuaFunction(args =>
            {
                if (args.Length > 0)
                {
                    var obj = args[0];
                    if (TryGetFileObject(obj, out _))
                    {
                        currentOutput = obj;
                    }
                    else if (obj.IsString)
                    {
                        currentOutput = io_open(Lua.Return(obj, "a"))[0];
                    }
                    else
                    {
                        GuardLibrary.ArgumentTypeError(args, 0, FileObject.TYPE_NAME, "output");
                    }
                }

                return Lua.Return(currentOutput);
            });

            var inputFunc = new LuaFunction(args =>
            {
                if (args.Length > 0)
                {
                    var obj = args[0];
                    if (TryGetFileObject(obj, out _))
                    {
                        currentInput = obj;
                    }
                    else if (obj.IsString)
                    {
                        currentInput = io_open(args)[0];
                    }
                    else
                    {
                        GuardLibrary.ArgumentTypeError(args, 0, FileObject.TYPE_NAME, "input");
                    }
                }

                return Lua.Return(currentInput);
            });

            io["close"] = new LuaFunction(args =>
            {
                FileObject output = null;
                if (args.Length == 0)
                {
                    if (TryGetFileObject(currentOutput, out var file))
                    {
                        output = file;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    if (TryGetFileObject(args[0], out var file))
                    {
                        output = file;
                    }
                    else
                    {
                        GuardLibrary.ArgumentTypeError(args, 0, FileObject.TYPE_NAME, "close");
                    }
                }
                output.Close();
                return Lua.Return();
            });
            io["flush"] = new LuaFunction(args =>
            {
                var file = outputFunc(args)[0];
                return file["flush"].MethodCall(file, Lua.Return());
            });
            io["input"] = new LuaFunction(inputFunc);
            io["lines"] = new LuaFunction(args =>
            {
                if (args.Length == 0)
                {
                    return currentOutput["lines"].Call("1");
                }
                else
                {
                    var file = io_open(Lua.Return(args[0]));
                    return file[0]["lines"].Call(args.Skip(1).ToArray());
                }
            });
            io["open"] = new LuaFunction(io_open);
            io["output"] = new LuaFunction(outputFunc);
            io["popen"] = new LuaFunction(io_popen);
            io["read"] = new LuaFunction(args =>
            {
                var file = inputFunc(Lua.Return())[0];
                return file["read"].MethodCall(file, args);
            });
            io["tmpfile"] = new LuaFunction(io_tmpfile);
            io["type"] = new LuaFunction(io_type);
            io["write"] = new LuaFunction(args =>
            {
                var file = outputFunc(Lua.Return())[0];
                return file["write"].MethodCall(file, args);
            });

            context.Set("io", io);
        }

        public class FileObject
        {
            public const string TYPE_NAME = "FILE*";

            public FileObject(Stream s)
            {
                Stream = s;
                if (s.CanRead)
                    Reader = new StreamReader(s);
                if (s.CanWrite)
                    Writer = new StreamWriter(s);
            }

            public FileObject(StreamReader reader, StreamWriter writer)
            {
                Reader = reader;
                Writer = writer;
            }

            public Stream Stream { get; set; }
            public StreamReader Reader { get; set; }
            public StreamWriter Writer { get; set; }

            public static LuaArguments Write(LuaArguments args)
            {
                var self = args[0];
                if (TryGetFileObject(self, out var fObj))
                {
                    foreach (var arg in args)
                    {
                        if (arg == self)
                            continue;

                        if (!(arg.IsString || arg.IsNumber))
                            Lua.Return();

                        if (fObj.Stream.CanWrite)
                            fObj.Writer.Write(arg.ToString());
                        else
                            Lua.Return();
                    }
                    return Lua.Return(self);
                }
                else
                    return Lua.Return();
            }

            public void Close()
            {
                Stream?.Close();
            }

            public static LuaArguments Lines(LuaArguments args)
            {
                if (TryGetFileObject(args[0], out var file))
                {
                    var readLine = new LuaFunction(args =>
                    {
                        var reader = file.Reader;
                        if (reader == null)
                        {
                            return Lua.Return(LuaObject.Nil);
                        }
                        var line = reader.ReadLine();
                        return Lua.Return(line);
                    });
                    return Lua.Return(readLine, LuaObject.Nil, LuaObject.Nil);
                }
                throw new InvalidOperationException();
            }

            public static LuaArguments Close(LuaArguments args)
            {
                var obj = args[0];
                if (TryGetFileObject(obj, out var file))
                {
                    file.Close();
                }
                return Lua.Return();
            }

            public static LuaArguments Flush(LuaArguments args)
            {
                var obj = args[0];
                if (TryGetFileObject(obj, out var file))
                {
                    file.Writer?.Flush();
                }
                return Lua.Return();
            }

            public static LuaArguments Seek(LuaArguments args)
            {
                var obj = args[0];
                var whence = args[1] | "cur";
                var offset = args[2] | 0;

                if (TryGetFileObject(obj, out var fObj))
                {
                    switch (whence.ToString())
                    {
                        case "cur":
                            fObj.Stream.Position += (long)offset; break;
                        case "set":
                            fObj.Stream.Position = (long)offset; break;
                        case "end":
                            fObj.Stream.Position = fObj.Stream.Length + (long)offset; break;
                    }
                    return Lua.Return(fObj.Stream.Position);
                }
                return Lua.Return();
            }

            public static LuaArguments Read(LuaArguments args)
            {
                var self = args[0];
                if (TryGetFileObject(self, out var file))
                {
                    if (args.Length == 1)
                    {
                        var line = file.Reader.ReadLine();
                        return Lua.Return(line);
                    }
                    else
                    {
                        var ret = new List<LuaObject>();
                        foreach (var arg in args)
                        {
                            if (arg == self)
                                continue;
                            if (arg.IsNumber)
                            {
                                var bld = new StringBuilder();
                                for (int i = 0; i < arg; i++)
                                {
                                    bld.Append((char)file.Reader.Read());
                                }
                                ret.Add(bld.ToString());
                            }
                            else if (arg == "a")
                                ret.Add(file.Reader.ReadToEnd());
                            else if (arg == "l")
                                ret.Add(file.Reader.ReadLine());
                            else if (arg == "L")
                            {
                                var bld = new StringBuilder();
                                bool hasBreakLine = false;
                                int c = file.Reader.Read();
                                while (c >= 0)
                                {
                                    if (hasBreakLine)
                                    {
                                        if (c != '\n' && c != '\r')
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (c == '\n' || c == '\r')
                                        {
                                            hasBreakLine = true;
                                        }
                                    }
                                    bld.Append((char)c);
                                    c = file.Reader.Read();
                                };
                                ret.Add(bld.ToString());
                            }
                            else if (arg == "n")
                            {
                                //TODO: Implement io.read("*n")
                                throw new NotImplementedException();
                            }
                        }
                        return Lua.Return(ret.ToArray());
                    }
                }
                else
                    return Lua.Return();
            }
        }

        public static bool TryGetFileObject(LuaObject obj, out FileObject file)
        {
            if (obj._luaObj is FileObject fileTemp)
            {
                file = fileTemp;
                return true;
            }

            file = null;
            return false;
        }

        LuaObject CreateFileObject(Stream stream)
        {
            LuaObject obj = LuaObject.FromObject(new FileObject(stream));
            obj.SetMetaTable(_fileMetaTable, true);

            return obj;
        }

        LuaArguments io_open(LuaArguments args)
        {
            var file = args[0];
            var mode = args[1];

            if (file.IsString)
            {
                FileMode fMode = FileMode.Open;
                FileAccess fAccess = FileAccess.Read;
                if (mode.IsString)
                {
                    var modeStr = mode.ToString().TrimEnd('b');
                    switch (modeStr)
                    {
                        case "r":
                            fAccess = FileAccess.Read; 
                            break;
                        case "r+":
                            fAccess = FileAccess.ReadWrite;
                            break;
                        case "w":
                            fMode = FileMode.Create; 
                            fAccess = FileAccess.Write;
                            break;
                        case "w+":
                            fMode = FileMode.Create;
                            fAccess = FileAccess.ReadWrite;
                            break;
                        case "a":
                            fMode = FileMode.Append;
                            fAccess = FileAccess.Write;
                            break;
                        case "a+":
                            fMode = FileMode.Append;
                            fAccess = FileAccess.ReadWrite;
                            break;
                    }
                }
                var stream = new FileStream(file.ToString(), fMode, fAccess);

                return Lua.Return(CreateFileObject(stream));
            }
            else
                return Lua.Return();
        }

        LuaArguments io_tmpfile(LuaArguments args)
        {
            var path = Path.GetTempFileName();
            var stream = new FileStream(path,
                FileMode.Append, 
                FileAccess.ReadWrite, 
                FileShare.Write,
                short.MaxValue, 
                FileOptions.DeleteOnClose);
            return Lua.Return(CreateFileObject(stream));
        }

        static LuaArguments io_type(LuaArguments args)
        {
            var obj = args[0];
            if (TryGetFileObject(obj, out var fObj))
            {
                var stream = fObj.Stream ?? fObj.Reader.BaseStream ?? fObj.Writer.BaseStream;
                if (fObj.Stream != null)
                {
                    if (!stream.CanWrite && !stream.CanRead)
                        return Lua.Return("closed file");
                    else
                        return Lua.Return("file");
                }
            }
            return Lua.Return("fail");
        }

        LuaArguments io_popen(LuaArguments args)
        {
            GuardLibrary.HasLengthAtLeast(args, 1, "popen");
            GuardLibrary.EnsureType(args, 0, LuaType.@string, "popen");
            var prog = args[0].AsString();
            var process = Process.Start(prog);

            var modeStr = "r";
            if (args.Length > 1)
            {
                GuardLibrary.EnsureType(args, 1, LuaType.@string, "popen");
                modeStr = args[1].AsString();
            }
            FileObject file = null;
            switch (modeStr)
            {
                case "r":
                    file = new FileObject(process.StandardOutput, null);
                    break;
                case "w":
                    file = new FileObject(null, process.StandardInput);
                    break;
            }
            if (file == null)
            {
                GuardLibrary.ArgumentError(2, "invalid file mode", "popen");
            }
            var obj = LuaObject.FromObject(file);
            obj.SetMetaTable(_fileMetaTable, true);
            return Lua.Return(obj);
        }
    }
}
