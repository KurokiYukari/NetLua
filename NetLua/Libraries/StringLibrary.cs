using System;
using System.Collections.Generic;
using System.Linq;

namespace NetLua
{
    public class StringLibrary : ILuaLibrary
    {
        public static StringLibrary Instance { get; } = new StringLibrary();

        public void AddLibrary(LuaContext context)
        {
            var lib = LuaObject.NewTable();
            lib["byte"] = LuaObject.FromFunction(Byte);
            lib["char"] = LuaObject.FromFunction(Char);
            lib["len"] = LuaObject.FromFunction(Len);
            lib["lower"] = LuaObject.FromFunction(Lower);
            lib["rep"] = LuaObject.FromFunction(Rep);
            lib["reverse"] = LuaObject.FromFunction(Reverse);
            lib["sub"] = LuaObject.FromFunction(Sub);
            lib["upper"] = LuaObject.FromFunction(Upper);

            context.Set("string", lib);
        }

        public LuaArguments Byte(string s, int i, int j)
        {
            if (i >= s.Length || i > j)
            {
                return Lua.Return();
            }

            var arr = new LuaObject[j - i + 1];
            for (int k = 0; k < arr.Length; k++)
            {
                arr[k] = (int)s[i + k];
            }

            return new LuaArguments(arr);
        }

        public LuaArguments Byte(LuaArguments args)
        {
            var s = GuardLibrary.EnsureString(args, 0, "byte");
            int i = 1;
            if (args.Length >= 2)
            {
                i = GuardLibrary.EnsureIntNumber(args, 1, "byte");
            }
            int j = i;
            if (args.Length >= 3)
            {
                j = GuardLibrary.EnsureIntNumber(args, 2, "byte");
            }
            return Byte(s, i, j);
        }

        public string Char(IEnumerable<int> args)
        {
            return new string(args.Select(i => (char)i).ToArray());
        }

        public LuaArguments Char(LuaArguments args)
        {
            var charSet = args.Select((c, i) => GuardLibrary.EnsureIntNumber(args, i, "char"));
            return Lua.Return(Char(charSet));
        }

        public LuaArguments Len(LuaArguments args)
        {
            var s = GuardLibrary.EnsureString(args, 0, "len");
            return Lua.Return(s.Length);
        }

        public LuaArguments Lower(LuaArguments args)
        {
            var s = GuardLibrary.EnsureString(args, 0, "lower");
            return Lua.Return(s.ToLower());
        }

        public LuaArguments Rep(LuaArguments args)
        {
            var s = GuardLibrary.EnsureString(args, 0, "rep");
            var n = GuardLibrary.EnsureIntNumber(args, 1, "rep");

            if (n <= 0)
            {
                return Lua.Return("");
            }

            var sep = "";
            if (args.Length > 2)
            {
                sep = GuardLibrary.EnsureString(args, 2, "rep");
            }
            
            return Lua.Return(string.Join(sep, Enumerable.Repeat(s, n)));
        }

        public LuaArguments Reverse(LuaArguments args)
        {
            var s = GuardLibrary.EnsureString(args, 0, "reverse");

            Span<char> span = stackalloc char[s.Length];
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = s[s.Length - 1 - i];
            }
            s = new string(span);
            return Lua.Return(s);
        }

        public LuaArguments Sub(LuaArguments args)
        {
            var s = GuardLibrary.EnsureString(args, 0, "sub");
            var i = GuardLibrary.EnsureIntNumber(args, 1, "sub");
            var j = -1;
            if (args.Length > 2)
            {
                j = GuardLibrary.EnsureIntNumber(args, 2, "sub");
            }

            if (j == 0)
            {
                return Lua.Return("");
            }
            i = ParseIndex(s, i);
            j = ParseIndex(s, j);

            return Lua.Return(s.Substring(i - 1, j - i + 1));
        }

        private static int ParseIndex(string s, int index)
        {
            if (index == 0)
            {
                return 1;
            }
            if (index < 0)
            {
                index = s.Length + index + 1;
            }

            if (index > s.Length)
            {
                index = s.Length;
            }
            return index;
        }

        public LuaArguments Upper(LuaArguments args)
        {
            var s = GuardLibrary.EnsureString(args, 0, "upper");
            return Lua.Return(s.ToUpper());
        }
    }
}