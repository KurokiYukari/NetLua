/*
 * See LICENSE file
 */

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using NetLua;

namespace LuaUnits
{
    [TestFixture]
    public class LuaEnvFunctionTest
    {
        [TestCase("""
function(...)
    return ...
end
""", "1", "2", TestName = "Varargs Raw")]
        [TestCase("""
function(...)
    local table = {...}
    return table[1], table[2]
end
""", "1", "2", TestName = "Varargs TableConstruct")]
        [TestCase("""
function(a, ...)
    return a, ...
end
""", "1", "2", TestName = "Varargs Mixed")]
        public void TestVarargs(string code, params string[] parameters)
        {
            var args = RunFunction(code, parameters);
            Assert.That(args, Has.Length.EqualTo(parameters.Length));
            Assert.That(args.Select(o => o.ToString()).SequenceEqual(parameters));
        }

        private static LuaArguments RunFunction(string code, params string[] parameters)
        {
            var fullCode = $"return ({code})({string.Join(", ", parameters)})";
            var lua = Lua.CreateDefaultEnv();
            return lua.DoString(fullCode);
        }
    }
}
