using System;
using System.Collections.Generic;
using System.Linq;

namespace NetLua
{
    public class TableLibrary : ILuaLibrary
    {
        public static TableLibrary Instance { get; } = new TableLibrary();

        public void AddLibrary(LuaContext context)
        {
            var table = LuaObject.NewTable();
            context.Set("table", table);

            table["concat"] = LuaObject.FromFunction(Concat);
            table["insert"] = LuaObject.FromFunction(Insert);
            table["move"] = LuaObject.FromFunction(Move);
            table["pack"] = LuaObject.FromFunction(args => Lua.Return(Pack(args)));
            table["remove"] = LuaObject.FromFunction(Remove);
            table["sort"] = LuaObject.FromFunction(Sort);
            table["unpack"] = LuaObject.FromFunction(Unpack);
        }

        public static string Concat(LuaObject list, string sep, int? i = null, int? j = null)
        {
            var table = GuardLibrary.EnsureTable(list, 1, "concat");
            i ??= 1;
            j ??= table.List.Count;
            if (i > j)
            {
                return string.Empty;
            }
            return string.Join(sep, table.List.Skip(i.Value - 1).Take(j.Value - i.Value + 1));
        }

        public static LuaArguments Concat(LuaArguments args)
        {
            var sep = args[1].AsString();
            int? i = null;
            if (args.Length >= 3)
            {
                i = (int)GuardLibrary.EnsureIntNumber(args, 2, "concat");
            }
            int? j = null;
            if (args.Length >= 4)
            {
                j = (int)GuardLibrary.EnsureIntNumber(args, 3, "concat");
            }
            return Lua.Return(Concat(args[0], sep, i, j));
        }

        public static void Insert(LuaObject obj, LuaObject item)
        {
            Insert(obj, null, item);
        }

        public static void Insert(LuaObject obj, int? index, LuaObject item)
        {
            var list = GuardLibrary.EnsureTable(obj, 1, "insert");
            index ??= list.Count + 1;
            list.Insert(index.Value, item);
        }

        public static LuaArguments Insert(LuaArguments args)
        {
            if (args.Length > 2)
            {
                var index = (int)GuardLibrary.EnsureIntNumber(args, 1, "insert");
                Insert(args[0], index, args[2]);
            }
            else
            {
                Insert(args[0], args[1]);
            }
            return Lua.Return();
        }

        public static LuaObject Move(LuaObject a1, int f, int e, int t, LuaObject a2)
        {
            GuardLibrary.EnsureTable(a1, 1, "move");
            if (a2.IsNil)
            {
                a2 = a1;
            }
            else
            {
                GuardLibrary.EnsureTable(a2, 5, "move");
            }

            if (f < e)
            {
                return a2;
            }

            for (int i = 0; i < e - f; i++)
            {
                a2[t + i] = a1[e + i];
            }

            return a2;
        }

        public static LuaArguments Move(LuaArguments args)
        {
            var f = GuardLibrary.EnsureIntNumber(args, 1, "move");
            var e = GuardLibrary.EnsureIntNumber(args, 2, "move");
            var t = GuardLibrary.EnsureIntNumber(args, 3, "move");
            return Lua.Return(Move(args[0], f, e, t, args[4]));
        }

        public static LuaObject Pack(LuaArguments args)
        {
            var table = LuaObject.NewTable();
            for (int i = 0; i < args.Length; i++)
            {
                table[i + 1] = args[i];
            }
            table["n"] = args.Length;
            return table;
        }

        public static LuaObject Remove(LuaObject table, int? pos = null)
        {
            var list = GuardLibrary.EnsureTable(table, 1, "remove");
            var len = list.List.Count;
            pos ??= len;
            if (len + 1 == pos.Value)
            {
                return LuaObject.Nil;
            }
            if (list.List.Count == 0 && pos.Value == 0)
            {
               return LuaObject.Nil;
            }

            var item = list[pos.Value];
            list.RemoveAt(pos.Value);
            return item;
        }

        public static LuaArguments Remove(LuaArguments args)
        {
            int? pos = null;
            if (args.Length >= 2)
            {
                pos = GuardLibrary.EnsureIntNumber(args, 1, "remove");
            }
            return Lua.Return(Remove(args[0], pos));
        }

        public static void Sort(LuaObject table, LuaObject comp = null)
        {
            var list = GuardLibrary.EnsureTable(table, 1, "sort");
            list.Sort((x, y) =>
            {
                if (comp == null)
                {
                    if (x == y)
                    {
                        return 0;
                    }
                    return x > y ? 1 : -1;
                }
                else
                {
                    var result = comp.Call(Lua.Return(x, y))[0];
                    if (result.IsNumber)
                    {
                        return (int)result.AsNumber();
                    }
                    else
                    {
                        var b = result.AsBool();
                        return b ? 1 : -1;
                    }
                }
            });
        }

        public static LuaArguments Sort(LuaArguments args)
        {
            Sort(args[0], args[1]);
            return Lua.Return();
        }

        public static LuaArguments Unpack(LuaObject table, int? i = null, int? j = null)
        {
            var list = GuardLibrary.EnsureTable(table, 1, "unpack");
            i ??= 1;
            j ??= list.List.Count;

            if (i > list.List.Count)
            {
                return Lua.Return(LuaObject.Nil);
            }
            if (i > j)
            {
                return Lua.Return();
            }

            return new LuaArguments(list.List.Skip(i.Value - 1).Take(j.Value - i.Value + 1).ToArray());
        }

        public static LuaArguments Unpack(LuaArguments args)
        {
            int? i = null;
            if (args.Length >= 2)
            {
                i = GuardLibrary.EnsureIntNumber(args, 1, "unpack");
            }
            int? j = null;
            if (args.Length >= 3)
            {
                j = GuardLibrary.EnsureIntNumber(args, 2, "unpack");
            }
            return Unpack(args[0], i, j);
        }
    }
}
