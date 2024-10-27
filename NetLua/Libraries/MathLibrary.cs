using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLua
{
    public class MathLibrary : ILuaLibrary
    {
        private Random _random = Random.Shared;

        public static MathLibrary Instance { get; } = new MathLibrary();

        public void AddLibrary(LuaContext Context)
        {
            var math = LuaObject.NewTable();
            math["abs"] = (LuaFunction)abs;
            math["acos"] = (LuaFunction)acos;
            math["asin"] = (LuaFunction)asin;
            math["atan"] = (LuaFunction)atan;
            math["atan2"] = (LuaFunction)atan2;
            math["ceil"] = (LuaFunction)ceil;
            math["cos"] = (LuaFunction)cos;
            math["cosh"] = (LuaFunction)cosh;
            math["deg"] = (LuaFunction)deg;
            math["exp"] = (LuaFunction)exp;
            math["floor"] = (LuaFunction)floor;
            math["fmod"] = (LuaFunction)fmod;
            math["huge"] = LuaObject.FromNumber(double.MaxValue);
            math["log"] = (LuaFunction)log;
            math["max"] = (LuaFunction)max;
            math["maxinteger"] = LuaObject.FromNumber(long.MaxValue);
            math["min"] = (LuaFunction)min;
            math["mininteger"] = LuaObject.FromNumber(long.MinValue);
            math["modf"] = (LuaFunction)modf;
            math["pi"] = Math.PI;
            math["pow"] = (LuaFunction)pow;
            math["rad"] = (LuaFunction)rad;
            math["random"] = (LuaFunction)random;
            math["randomseed"] = (LuaFunction)randomseed;
            math["sin"] = (LuaFunction)sin;
            math["sinh"] = (LuaFunction)sinh;
            math["sqrt"] = (LuaFunction)sqrt;
            math["tan"] = (LuaFunction)tan;
            math["tointeger"] = (LuaFunction)tointeger;
            math["tanh"] = (LuaFunction)tanh;
            math["type"] = (LuaFunction)type;
            math["ult"] = (LuaFunction)ult;

            Context.Set("math", math);
        }

        static LuaArguments abs(LuaArguments args)
        {
            return Lua.Return(Math.Abs(args[0]));
        }

        static LuaArguments acos(LuaArguments args)
        {
            return Lua.Return(Math.Acos(args[0]));
        }

        static LuaArguments asin(LuaArguments args)
        {
            return Lua.Return(Math.Asin(args[0]));
        }

        static LuaArguments atan(LuaArguments args)
        {
            return Lua.Return(Math.Atan(args[0]));
        }

        static LuaArguments atan2(LuaArguments args)
        {
            return Lua.Return(Math.Atan2(args[0], args[1]));
        }

        static LuaArguments ceil(LuaArguments args)
        {
            return Lua.Return(Math.Ceiling(args[0]));
        }

        static LuaArguments cos(LuaArguments args)
        {
            return Lua.Return(Math.Cos(args[0]));
        }

        static LuaArguments cosh(LuaArguments args)
        {
            return Lua.Return(Math.Cosh(args[0]));
        }

        static LuaArguments deg(LuaArguments args)
        {
            var radius = GuardLibrary.EnsureNumber(args, 0, "deg");
            return Lua.Return(radius / Math.PI * 180);
        }

        static LuaArguments exp(LuaArguments args)
        {
            return Lua.Return(Math.Exp(args[0]));
        }

        static LuaArguments floor(LuaArguments args)
        {
            return Lua.Return(Math.Floor(args[0]));
        }

        static LuaArguments fmod(LuaArguments args)
        {
            var x = GuardLibrary.EnsureNumber(args, 0, nameof(fmod));
            var y = GuardLibrary.EnsureNumber(args, 1, nameof(fmod));
            if (y == 0)
            {
                GuardLibrary.ArgumentError(2, nameof(fmod), "zero");
            }
            var factor = (int)(x / y);
            return Lua.Return(x - factor * y);
        }

        static LuaArguments log(LuaArguments args)
        {
            return Lua.Return(Math.Log(args[0], args[1] | Math.E));
        }

        static LuaArguments max(LuaArguments args)
        {
            var max = args[0];
            foreach (LuaObject o in args)
            {
                max = Math.Max(max, o);
            }
            return Lua.Return(max);
        }

        static LuaArguments min(LuaArguments args)
        {
            var min = args[0];
            foreach (LuaObject o in args)
            {
                min = Math.Min(min, o);
            }
            return Lua.Return(min);
        }

        static LuaArguments modf(LuaArguments args)
        {
            var x = GuardLibrary.EnsureNumber(args, 0, nameof(modf));
            var intPart = (int)x;
            var floatPart = x - intPart;
            return Lua.Return(intPart, floatPart);
        }

        static LuaArguments rad(LuaArguments args)
        {
            var x = GuardLibrary.EnsureNumber(args, 0, nameof(rad));
            return Lua.Return(x / 180 * Math.PI);
        }

        static LuaArguments pow(LuaArguments args)
        {
            return Lua.Return(Math.Pow(args[0], args[1]));
        }

        LuaArguments random(LuaArguments args)
        {
            var random = _random;
            if (args.Length == 0)
            {
                return Lua.Return(Random.Shared.NextDouble());
            }
            var m = GuardLibrary.EnsureIntNumber(args, 0, nameof(random));
            if (args.Length == 1)
            {
                if (m == 0)
                {
                    return Lua.Return(LuaObject.FromNumber(random.NextInt64(long.MinValue, long.MaxValue)));
                }
                else
                {
                    return Lua.Return(LuaObject.FromNumber(random.NextInt64(1, m)));
                }
            }
            else
            {
                var n = GuardLibrary.EnsureIntNumber(args, 1, nameof(random));
                return Lua.Return(LuaObject.FromNumber(random.NextInt64(m, n)));
            }
        }

        LuaArguments randomseed(LuaArguments args)
        {
            Random random;
            if (args.Length == 0)
            {
                random = Random.Shared;
            }
            else if (args.Length == 1)
            {
                var x = GuardLibrary.EnsureIntNumber(args, 0, nameof(randomseed));
                random = new Random((int)x);
            }
            else
            {
                var x = GuardLibrary.EnsureIntNumber(args, 0, nameof(randomseed));
                var y = GuardLibrary.EnsureIntNumber(args, 1, nameof(randomseed));
                random = new Random((int)x ^ (int)y);
            }
            _random = random;
            return Lua.Return(LuaObject.FromNumber(random.NextInt64()), LuaObject.FromNumber(random.NextInt64()));
        }

        static LuaArguments sin(LuaArguments args)
        {
            return Lua.Return(Math.Sin(args[0]));
        }

        static LuaArguments sinh(LuaArguments args)
        {
            return Lua.Return(Math.Sinh(args[0]));
        }

        static LuaArguments sqrt(LuaArguments args)
        {
            return Lua.Return(Math.Sqrt(args[0]));
        }

        static LuaArguments tan(LuaArguments args)
        {
            return Lua.Return(Math.Tan(args[0]));
        }

        static LuaArguments tointeger(LuaArguments args)
        {
            GuardLibrary.HasLengthAtLeast(args, 1, nameof(tointeger));
            if (args[0].TryConvertToInt(out var i))
            {
                return Lua.Return(LuaObject.FromNumber(i));
            }
            return Lua.Return(LuaObject.Nil);
        }

        static LuaArguments tanh(LuaArguments args)
        {
            return Lua.Return(Math.Tanh(args[0]));
        }

        static LuaArguments type(LuaArguments args)
        {
            GuardLibrary.HasLengthAtLeast(args, 1, nameof(type));
            var x = args[0];
            if (x.type == LuaType.number)
            {
                if (x._luaObj is long)
                {
                    return Lua.Return("integer");
                }
                else
                {
                    return Lua.Return("float");
                }
            }

            return Lua.Return(LuaObject.Nil);
        }

        static LuaArguments ult(LuaArguments args)
        {
            GuardLibrary.HasLengthAtLeast(args, 2, nameof(ult));
            var m = GuardLibrary.EnsureIntNumber(args, 0, nameof(ult));
            var n = GuardLibrary.EnsureIntNumber(args, 1, nameof(ult));
            if (m < 0)
            {
                return Lua.Return(LuaObject.False);
            }
            else
            {
                return Lua.Return(m < n);
            }
        }
    }
}
