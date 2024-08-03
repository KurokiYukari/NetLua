/*
 * See LICENSE file
 */

using System.Linq.Expressions;
using NetLua;

namespace LuaUnits
{
    [TestFixture]
    public class LuaEnvOperatorTest
    {
        private static IEnumerable<object[]> GetRightZeroBinaryNumberParameters()
        {
            yield return ["0", "0"];
            yield return ["0", "0"];
        }

        private static IEnumerable<object[]> GetGenericBinaryNumberParameters()
        {
            yield return ["0", "2"];
            yield return ["1", "1"];
            yield return ["1.0", "2.0"];
            yield return ["1.23", "1"];
            yield return ["1.23", "2.23"];
            yield return ["1.23", "-2.23"];
            yield return ["-1.23", "2.23"];
            yield return ["-1.23", "-2.23"];
        }

        private static IEnumerable<object[]> GetAllBinaryNUmberParameters()
        {
            return GetRightZeroBinaryNumberParameters()
                .Concat(GetGenericBinaryNumberParameters());
        }

        [TestCaseSource(nameof(GetGenericBinaryNumberParameters))]
        public void Divide(string l, string r)
        {
            var fullCode = $"{l} / {r}";

            var actual = TryRun(fullCode, true);
            if (actual != null)
            {
                Assert.That(actual.AsNumber(),
                    Is.EqualTo(double.Parse(l) / double.Parse(r)));
            }
        }

        [TestCaseSource(nameof(GetAllBinaryNUmberParameters))]
        public void Modulo(string l, string r)
        {
            var fullCode = $"{l} % {r}";

            var actual = TryRun(fullCode, true);
            if (actual != null)
            {
                var ld = double.Parse(l);
                var rd = double.Parse(r);
                Assert.That(actual.AsNumber(),
                    Is.EqualTo(ld - Math.Floor(ld / rd) * rd));
            }
        }

        [TestCaseSource(nameof(GetAllBinaryNUmberParameters))]
        public void Multiply(string l, string r)
        {
            var fullCode = $"{l} * {r}";

            var actual = TryRun(fullCode, true);
            if (actual != null)
            {
                Assert.That(actual.AsNumber(),
                    Is.EqualTo(double.Parse(l) * double.Parse(r)));
            }
        }

        [TestCaseSource(nameof(GetAllBinaryNUmberParameters))]
        public void Power(string l, string r)
        {
            var fullCode = $"{l} ^ {r}";

            var actual = TryRun(fullCode, true);
            if (actual != null)
            {
                Assert.That(actual.AsNumber(), 
                    Is.EqualTo(Math.Pow(
                        double.Parse(l),
                        double.Parse(r))));
            }
        }

        [TestCaseSource(nameof(GetAllBinaryNUmberParameters))]
        public void Subtract(string l, string r)
        {
            var fullCode = $"{l} - {r}";

            var actual = TryRun(fullCode, true);
            if (actual != null)
            {
                Assert.That(actual.AsNumber(), 
                    Is.EqualTo(double.Parse(l) - double.Parse(r)));
            }
        }

        [TestCase("0", "0", 0 << 0)]
        [TestCase("0", "2", 0 << 2)]
        [TestCase("1", "1", 1 << 1)]
        [TestCase("1.0", "2.0", 1 << 2)]
        [TestCase("1.23", "1", 0, false)]
        public void LeftShift(string l, string r, int excepted, bool isValid = true)
        {
            var fullCode = $"{l} << {r}";

            var actual = TryRun(fullCode, isValid);
            if (actual != null)
            {
                Assert.That((int)actual.AsNumber(), Is.EqualTo(excepted));
            }
        }

        [TestCase("0", "0", 0 >> 0)]
        [TestCase("0", "2", 0 >> 2)]
        [TestCase("1", "1", 1 >> 1)]
        [TestCase("2.0", "1.0", 2 >> 1)]
        [TestCase("1.23", "1", 0, false)]
        public void RightShift(string l, string r, int excepted, bool isValid = true)
        {
            var fullCode = $"{l} >> {r}";

            var actual = TryRun(fullCode, isValid);
            if (actual != null)
            {
                Assert.That((int)actual.AsNumber(), Is.EqualTo(excepted));
            }
        }

        [TestCase("0", "0", 0 & 0)]
        [TestCase("0", "2", 0 & 2)]
        [TestCase("1", "1", 1 & 1)]
        [TestCase("1.0", "2.0", 1 & 2)]
        [TestCase("1.23", "1", 0, false)]
        public void BitwiseAnd(string l, string r, int excepted, bool isValid = true)
        {
            var fullCode = $"{l} & {r}";

            var actual = TryRun(fullCode, isValid);
            if (actual != null)
            {
                Assert.That((int)actual.AsNumber(), Is.EqualTo(excepted));
            }
        }

        [TestCase("0", "0", 0 ^ 0)]
        [TestCase("0", "2", 0 ^ 2)]
        [TestCase("1", "1", 1 ^ 1)]
        [TestCase("1.0", "2.0", 1 ^ 2)]
        [TestCase("1.23", "1", 0, false)]
        public void BitwiseExclusiveOr(string l, string r, int excepted, bool isValid = true)
        {
            var fullCode = $"{l} ~ {r}";

            var actual = TryRun(fullCode, isValid);
            if (actual != null)
            {
                Assert.That((int)actual.AsNumber(), Is.EqualTo(excepted));
            }
        }

        [TestCase("0", "0", 0 | 0)]
        [TestCase("0", "2", 0 | 2)]
        [TestCase("1", "1", 1 | 1)]
        [TestCase("1", "2", 1 | 2)]
        [TestCase("1.23", "1", 0, false)]
        public void BitwiseOr(string l, string r, int excepted, bool isValid = true)
        {
            var fullCode = $"{l} | {r}";

            var actual = TryRun(fullCode, isValid);
            if (actual != null)
            {
                Assert.That((int)actual.AsNumber(), Is.EqualTo(excepted));
            }
        }

        private static LuaObject? TryRun(string code, bool isValid)
        {
            if (isValid)
            {
                return Run(code);
            }
            else
            {
                Assert.Catch<Exception>(() =>
                {
                    Run(code);
                });
                return null;
            }
        }

        private static LuaObject Run(string code)
        {
            var lua = new Lua();
            return lua.DoString($"return\r\n{code}")[0];
        }
    }
}
