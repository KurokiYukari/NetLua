/*
 * See LICENSE file
 */

namespace NetLua
{
    public static class LuaExtensions
    {
        public static int GetInt(this LuaObject obj, LuaObject index)
        {
            if (obj[index].TryConvertToInt(out var value))
            {
                return value;
            }

            throw new LuaException($"field '{index}' is not an integer");
        }
    }
}
