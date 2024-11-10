using System;

namespace NetLua
{
    public class TableLibrary : ILuaLibrary
    {
        public void AddLibrary(LuaContext context)
        {
            throw new NotImplementedException();
        }

        public static void Insert(LuaObject obj, LuaObject item)
        {
            GuardLibrary.EnsureTable(obj, 1, "insert");
            var len = obj.Len();
            obj[len + 1] = item;
        }
    }
}
