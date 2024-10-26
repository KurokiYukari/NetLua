/*
 * See LICENSE file
 */

using System;

namespace NetLua
{
    public static class Lua
    {
        private static readonly LuaObject[] _defaultReturn = new LuaObject[] { LuaObject.Nil };
        /// <summary>
        /// Helper function for returning Nil from a function
        /// </summary>
        /// <returns>Nil</returns>
        public static LuaArguments Return()
        {
            return _defaultReturn;
        }

        /// <summary>
        /// Helper function for returning objects from a function
        /// </summary>
        /// <param name="values">The objects to return</param>
        public static LuaArguments Return(params LuaObject[] values)
        {
            return values;
        }

        public static LuaContext CreateDefaultEnv()
        {
            return CreateDefaultEnv(null);
        }
        public static LuaContext CreateDefaultEnv(Func<Parser> parserGetter)
        {
            var context = new LuaContext(null, null, parserGetter);
            BasicLibrary.Instance.AddLibrary(context);
            return context;
        }
    }
}
