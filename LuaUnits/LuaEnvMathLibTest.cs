/*
 * See LICENSE file
 */

using NetLua;

namespace LuaUnits
{
    [TestFixture]
    public class LuaEnvMathLibTest
    {
        [TestCase]
        public void MaxInteger()
        {
            var MAX = 9223372036854775807;
            var maxInteger = (long)Run("math.maxinteger").AsUserData();
            Assert.That(maxInteger, Is.EqualTo(MAX));
        }

        [TestCase]
        public void MinInteger()
        {
            var MIN = -9223372036854775808;
            var minInteger = (long)Run("math.mininteger").AsUserData();
            Assert.That(minInteger, Is.EqualTo(MIN));
        }

        private static LuaObject Run(string code)
        {
            var lua = Lua.CreateDefaultEnv();
            return lua.DoString($"return\r\n{code}")[0];
        }
    }
}
