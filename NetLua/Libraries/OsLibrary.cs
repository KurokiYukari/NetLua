using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace NetLua
{
    public class OsLibrary : ILuaLibrary
    {
        private readonly long _startTime;

        public OsLibrary()
        {
            _startTime = DateTime.Now.Millisecond;
        }

        public void AddLibrary(LuaContext context)
        {
            var lib = LuaObject.NewTable();
            lib["clock"] = LuaObject.FromFunction(Clock);
            lib["date"] = LuaObject.FromFunction(Date);
            lib["difftime"] = LuaObject.FromFunction(DiffTime);
            lib["time"] = LuaObject.FromFunction(Time);
            lib["execute"] = LuaObject.FromFunction(args =>
            {
                string command = null;
                if (args.Length >= 1)
                {
                    command = GuardLibrary.EnsureString(args, 0, "execute");
                }
                return Execute(command);
            });
            lib["getenv"] = LuaObject.FromFunction(GetEnv);
            lib["remove"] = LuaObject.FromFunction(Remove);
            lib["rename"] = LuaObject.FromFunction(Rename);
            lib["setlocale"] = LuaObject.FromFunction(SetLocale);
            lib["tmpname"] = LuaObject.FromFunction(TmpName);
        }

        public double Clock()
        {
            var currentTime = DateTime.Now.Millisecond;
            return (double)(currentTime - _startTime) / 1000;
        }

        public LuaArguments Clock(LuaArguments args)
        {
            return Lua.Return(Clock());
        }

        public static LuaArguments Date(LuaArguments args)
        {
            DateTime time;
            string format = "";

            if (args.Length >= 1)
            {
                format = GuardLibrary.EnsureString(args, 0, "date");
            }

            var formatSpan = format.AsSpan();

            if (args.Length >= 2)
            {
                var timeTick = GuardLibrary.EnsureLongNumber(args, 1, "date");
                time = new DateTime(timeTick);
            }
            else
            {
                time = DateTime.Now;
            }

            if (formatSpan.IsEmpty)
            {
                return Lua.Return(DateTime.Now.ToString());
            }

            if (formatSpan.StartsWith("!"))
            {
                formatSpan = formatSpan[1..];
                time = time.ToUniversalTime();
            }

            if (formatSpan.StartsWith("*t"))
            {
                var table = BuildTimeTable(time, true);
                table["isdst"] = LuaObject.FromBool(false);

                return Lua.Return(table);
            }

            return Lua.Return(time.ToString(formatSpan.ToString()));
        }

        public static long Time(LuaObject table)
        {
            if (table.IsNil)
            {
                return DateTime.Now.Ticks;
            }

            var year = table.GetInt("year");
            var month = table.GetInt("month");
            var day = table.GetInt("hour");

            var date = new DateTime(year, month, day);
            if (table["hour"].TryConvertToNumber(out var hour))
            {
                date = date.AddHours(hour);
            }
            if (table["min"].TryConvertToNumber(out var min))
            {
                date = date.AddMinutes(min);
            }
            if (table["sec"].TryConvertToNumber(out var sec))
            {
                date = date.AddSeconds(sec);
            }

            BuildTimeTable(table, date, false);

            return date.Ticks;
        }

        public static LuaArguments Time(LuaArguments args)
        {
            return Lua.Return(Time(args[0]));
        }

        public static int DiffTime(long t2, long t1)
        {
            var deltaTicks = t2 - t1;
            var offset = new TimeSpan(deltaTicks);
            return offset.Seconds;
        }

        public static LuaArguments DiffTime(LuaArguments args)
        {
            var t2 = GuardLibrary.EnsureLongNumber(args, 0, "difftime");
            var t1 = GuardLibrary.EnsureLongNumber(args, 1, "difftime");

            return Lua.Return(DiffTime(t2, t1));
        }

        public static LuaArguments Execute(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return Lua.Return(LuaObject.True);
            }

            try
            {
                var process = Process.Start(command);
                process.WaitForExit();

                return Lua.Return(LuaObject.True, "exit", process.ExitCode);
            }
            catch (Exception)
            {
                return Lua.Return(LuaObject.Nil, "exit", 1);
            }
        }

        public static LuaArguments GetEnv(LuaArguments args)
        {
            var name = GuardLibrary.EnsureString(args, 0, "getenv");
            return Lua.Return(name);
        }

        public static LuaArguments Remove(LuaArguments args)
        {
            var name = GuardLibrary.EnsureString(args, 0, "remove");
            try
            {
                if (File.Exists(name))
                {
                    File.Delete(name);
                    return Lua.Return(LuaObject.True);
                }
                else if (Directory.Exists(name))
                {
                    Directory.Delete(name);
                    return Lua.Return(LuaObject.True);
                }
            }
            catch (Exception e)
            {
                return Lua.Return(LuaObject.Nil, e.Message, 1);
            }

            return Lua.Return(LuaObject.Nil, $"{name}: No such file or directory", 2);
        }

        public static LuaArguments Rename(LuaArguments args)
        {
            var oldName = GuardLibrary.EnsureString(args, 0, "rename");
            var newName = GuardLibrary.EnsureString(args, 1, "rename");
            try
            {
                File.Move(oldName, newName);
            }
            catch (Exception e)
            {
                return Lua.Return(LuaObject.Nil, e.Message, 1);
            }
            return Lua.Return(LuaObject.True);
        }

        public static LuaArguments SetLocale(LuaArguments args)
        {
            var localeObj = args[0];
            var culture = CultureInfo.CurrentCulture;
            if (localeObj.TryConvertToString(out var localeStr))
            {
                if (localeStr == "C")
                {
                    culture = CultureInfo.InvariantCulture;
                }
                else
                {
                    try
                    {
                        culture = CultureInfo.GetCultureInfo(localeStr);
                    }
                    catch (Exception)
                    {
                        return Lua.Return(LuaObject.Nil);
                    }
                }
            }
            else if (!localeObj.IsNil)
            {
                return Lua.Return(LuaObject.Nil);
            }

            return Lua.Return(culture.Name);
        }

        public static LuaArguments TmpName(LuaArguments args)
        {
            var file = Path.GetTempFileName();
            return Lua.Return(file);
        }

        public static void BuildTimeTable(LuaObject table, DateTime time, bool addExternalInfo)
        {
            table["year"] = LuaObject.FromNumber(time.Year);
            table["month"] = LuaObject.FromNumber(time.Month);
            table["day"] = LuaObject.FromNumber(time.Day);
            table["hour"] = LuaObject.FromNumber(time.Hour);
            table["min"] = LuaObject.FromNumber(time.Minute);
            table["sec"] = LuaObject.FromNumber(time.Second);
            if (addExternalInfo)
            {
                table["wday"] = LuaObject.FromNumber((int)time.DayOfWeek + 1);
                table["yday"] = LuaObject.FromNumber(time.DayOfYear);
            }
        }

        public static LuaObject BuildTimeTable(DateTime time, bool addExternalInfo)
        {
            var table = LuaObject.NewTable();
            BuildTimeTable(table, time, addExternalInfo);
            return table;
        }
    }
}