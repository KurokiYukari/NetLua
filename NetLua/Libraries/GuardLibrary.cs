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

        public static void ArgumentError(int index, string name, string message)
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
    }
}