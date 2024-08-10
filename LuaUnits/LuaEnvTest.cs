/*
 * See LICENSE file
 */

using NetLua;

namespace LuaUnits
{
    [TestFixture]
    public class LuaEnvTest
    {
        [Test]
        public void TestEnvAndG()
        {
            var env = new Lua();
            
            env.DoString("a = 1");

            var _G = env.Context.Get("_G");
            var _G_G = _G["_G"];
            Assert.That(_G_G, Is.EqualTo(_G));

            var _ENV = env.Context.Get("_ENV");
            Assert.That(_ENV, Is.EqualTo(_G));

            var a = env.Context.Get("a");
            Assert.That(a.AsNumber(), Is.EqualTo(1));

            var e_a = _ENV["a"];
            Assert.That(e_a, Is.EqualTo(a));

            var b = env.DoString("local b = 2 return b")[0];

            Assert.That(b.AsNumber(), Is.EqualTo(2));

            b = _ENV["b"];
            Assert.That(b, Is.EqualTo(LuaObject.Nil));

            env.DoString("function foo() _ENV = {} return a end");
            a = env.DoString("foo()")[0];
            Assert.That(a, Is.EqualTo(LuaObject.Nil));

            a = env.Context.Get("a");
            Assert.That(a.AsNumber(), Is.EqualTo(1));
        }
    }
}
