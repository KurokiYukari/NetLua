using System;
using System.Collections.Generic;
using System.Reflection;

namespace NetLua
{
    public class LuaMetaTableBuilder
    {
        private static readonly Type _baseBuilderType = typeof(LuaMetaTableBuilder);
        private readonly Lazy<IReadOnlyCollection<(string eventName, LuaFunction method)>> _methods;

        public LuaMetaTableBuilder()
        {
            _methods = new Lazy<IReadOnlyCollection<(string, LuaFunction)>>(() =>
            {
                var result = new List<(string, LuaFunction)>();
                var type = GetType();

                void TryAdd(string eventName, LuaFunction method)
                {
                    if (method.Method.DeclaringType != _baseBuilderType)
                    {
                        result.Add((eventName, method));
                    }
                }

                TryAdd(LuaEvents.ADD, Add);
                TryAdd(LuaEvents.SUB, Sub);
                TryAdd(LuaEvents.MUL, Mul);
                TryAdd(LuaEvents.DIV, Div);
                TryAdd(LuaEvents.MOD, Mod);
                TryAdd(LuaEvents.POW, Pow);
                TryAdd(LuaEvents.UNM, Unm);
                TryAdd(LuaEvents.BITWISE_AND, BitwiseAnd);
                TryAdd(LuaEvents.BITWISE_OR, BitwiseOr);
                TryAdd(LuaEvents.BITWISE_XOR, BitwiseXor);
                TryAdd(LuaEvents.BITWISE_NOT, BitwiseNot);
                TryAdd(LuaEvents.SHIFT_LEFT, ShiftLeft);
                TryAdd(LuaEvents.SHIFT_RIGHT, ShiftRight);
                TryAdd(LuaEvents.INDEX, Index);
                TryAdd(LuaEvents.NEW_INDEX, NewIndex);
                TryAdd(LuaEvents.CALL, Call);
                TryAdd(LuaEvents.CONCAT, Concat);
                TryAdd(LuaEvents.EQUAL, Equal);
                TryAdd(LuaEvents.LESS, Less);
                TryAdd(LuaEvents.LESS_OR_EQUAL, LessOrEqual);
                TryAdd(LuaEvents.GC, GC);
                TryAdd(LuaEvents.TO_STRING, ToString);
                TryAdd(LuaEvents.PAIRS, Pairs);

                return result;
            }, true);
        }

        public void BuildMetaTable(LuaObject metaTable)
        {
            GuardLibrary.EnsureType(metaTable, 0, LuaType.table, "metaTable");

            foreach (var (eventName, method) in _methods.Value)
            {
                metaTable[eventName] = method;
            }
        }

        public void BuildMetaTableFor(LuaObject target)
        {
            var metaTable = target.GetMetaTable();
            BuildMetaTable(metaTable);
        }

        public virtual LuaArguments Add(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Sub(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Mul(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Div(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Mod(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Pow(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Unm(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments BitwiseAnd(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments BitwiseOr(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments BitwiseXor(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments BitwiseNot(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments ShiftLeft(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments ShiftRight(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Index(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments NewIndex(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Call(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Concat(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Equal(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Less(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments LessOrEqual(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments GC(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments ToString(LuaArguments args)
        {
            throw new NotImplementedException();
        }

        public virtual LuaArguments Pairs(LuaArguments args)
        {
            throw new NotImplementedException();
        }
    }
}