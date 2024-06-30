/*
 * See LICENSE file
 */

using NetLua;
using NetLua.Ast;

namespace LuaUnits
{
    [TestFixture]
    public class LuaParserTests
    {
        private readonly Parser _parser = new();

        [Test]
        public void ParseNil()
        {
            ParseExpression<NilLiteral>("nil");
        }

        [TestCase("0")]
        [TestCase("1")]
        [TestCase("9223372036854775807")] // long max
        [TestCase("-9223372036854775808")]
        [TestCase("0.0")]
        [TestCase("1.7976931348623157E+308")] // double max
        public void ParseNumber(string numberStr)
        {
            var literal = ParseExpression<NumberLiteral>(numberStr);
            Assert.That(literal.Value, Is.EqualTo(double.Parse(numberStr)));
        }

        [TestCase("false")]
        [TestCase("true")]
        public void ParseBool(string boolStr)
        {
            var literal = ParseExpression<BoolLiteral>(boolStr);
            Assert.That(literal.Value, Is.EqualTo(bool.Parse(boolStr)));
        }

        [TestCase(@"""""", "")]
        [TestCase(@"''", "")]
        [TestCase(@"""Q'w'Q""", "Q'w'Q")]
        [TestCase(@"'Q""A""Q'", @"Q""A""Q")]
        public void ParseString(string codeStr, string realStr)
        {
            var literal = ParseExpression<StringLiteral>(codeStr);
            Assert.That(literal.Value, Is.EqualTo(realStr));
        }

        [TestCase("True")]
        [TestCase("False")]
        [TestCase("_")]
        [TestCase("arg")]
        [TestCase("Q.A.Q")]
        public void ParseVariable(string variableStr)
        {
            ParseExpression<Variable>(variableStr);
        }

        [TestCase("function() end")]
        [TestCase("function(...) end")]
        [TestCase("function(a) end")]
        [TestCase("function(a, ...) end")]
        public void ParseFunctionDefinition(string code)
        {
            ParseExpression<FunctionDefinition>(code);
        }

        private IExpression ParseExpression(string code)
        {
            var block = _parser.ParseString($"return\r\n{code}");
            return ((ReturnStat)block.Statements[0]).Expressions[0];
        }

        private TExpr ParseExpression<TExpr>(string code) where TExpr : IExpression
        {
            return (TExpr)ParseExpression(code);
        }
    }
}
