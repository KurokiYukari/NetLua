using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NetLua
{
    public class GuardLibrary : ILuaLibrary
    {
        public const string INDEX_OUT_OF_RANGE = "index out of range";
        public const string NOT_INT_NUMBER = "number has no integer representation";

        public void AddLibrary(LuaContext context)
        {

        }

        public static void HasLengthAtLeast(LuaArguments args, int length, string name = null)
        {
            if (args.Length < length)
            {
                BasicLibrary.Error($"bad argument length {args.Length} {(name == null ? string.Empty : $"'{name}' ")}(At least {length} expected)");
            }
        }

        [DoesNotReturn]
        public static void ArgumentError(int index, string message, string name)
        {
            BasicLibrary.Error($"bad argument #{index} to '{name}' ({message})");
        }

        public static void EnsureType(LuaArguments args, int index, LuaType type, string name)
        {
            EnsureType(args[index], index, type, name);
        }

        public static void EnsureType(LuaObject arg, int index, LuaType type, string name)
        {
            if (arg.Type != type)
            {
                BasicLibrary.Error($"bad argument #{index + 1} {(name == null ? string.Empty : $"'{name}' ")}({type} expected, got {arg.Type})");
            }
        }

        [DoesNotReturn]
        public static void ArgumentTypeError(LuaArguments args, int index, string type, string name)
        {
            ArgumentError(index + 1, $"{type} excepted, got {args[index].Type}", name);
        }

        public static ILuaTable EnsureTable(LuaArguments args, int index, string name)
        {
            var arg = args[index];
            if (!arg.IsTable)
            {
                ArgumentTypeError(args, index, LuaType.table.ToString(), name);
            }

            return arg.AsTable();
        }

        public static ILuaTable EnsureTable(LuaObject arg, int index, string name)
        {
            if (!arg.IsTable)
            {
                ArgumentError(index + 1, $"table excepted, got {arg.Type}", name);
            }

            return arg.AsTable();
        }

        public static double EnsureNumber(LuaArguments args, int index, string name)
        {
            var arg = args[index];
            if (arg.TryConvertToNumber(out var value))
            {
                return value;
            }

            ArgumentTypeError(args, index, LuaType.number.ToString(), name);
            return 0;
        }

        public static int EnsureIntNumber(LuaArguments args, int index, string name)
        {
            var arg = args[index];
            if (arg.TryConvertToInt(out var value))
            {
                return value;
            }

            if (arg.IsNumber)
            {
                ArgumentError(index + 1, NOT_INT_NUMBER, name);
            }
            else
            {
                ArgumentTypeError(args, index, LuaType.number.ToString(), name);
            }
            return 0;
        }

        public static string EnsureString(LuaArguments args, int index, string name)
        {
            var arg = args[index];
            if (arg.TryConvertToString(out var value))
            {
                return value;
            }

            ArgumentTypeError(args, index, LuaType.@string.ToString(), name);
            return "";
        }
    }
}