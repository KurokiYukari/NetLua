/*
 * See LICENSE file
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NetLua
{
    using LuaTableItem = KeyValuePair<LuaObject, LuaObject>;

    /// <summary>
    /// An exception thrown by the Lua interpreter
    /// </summary>
    public class LuaException : Exception
    {
        /// <summary>
        /// An exception thrown by a syntactical error
        /// </summary>
        /// <param name="file">The file witch contains the error</param>
        /// <param name="row">The row of the error</param>
        /// <param name="col">The column of the error</param>
        /// <param name="message">The kind of the error</param>
        public LuaException(string file, int row, int col, string message)
            : base(string.Format("Error in {0}({1},{2}): {3}", file, row, col, message))
        { }

        public LuaException(string message)
            : base("Error (unknown context): " + message)
        { }
    }

    /// <summary>
    /// A Lua function
    /// </summary>
    public delegate LuaArguments LuaFunction(LuaArguments args);

    public class LuaArguments : IEnumerable<LuaObject>
    {
        List<LuaObject> list;

        public LuaArguments()
        {
            list = new List<LuaObject>();
        }

        public LuaArguments(List<LuaObject> List)
        {
            list = List;
        }

        public LuaArguments(params LuaObject[] Objects)
        {
            list = new List<LuaObject>(Objects);
        }

        public LuaArguments(params LuaArguments[] Objects)
        {
            foreach (LuaArguments arg in Objects)
            {
                if (list == null)
                    list = new List<LuaObject>(arg.list);
                else
                    list.AddRange(arg.list);
            }
        }

        public int Length
        {
            get
            {
                return list.Count;
            }
        }

        public LuaObject this[int Index]
        {
            get
            {
                if (Index < list.Count)
                    return list[Index];
                else
                    return LuaObject.Nil;
            }
            set
            {
                if (Index < list.Count)
                    list[Index] = value;
            }
        }

        public void Add(LuaObject obj)
        {
            list.Add(obj);
        }

        public LuaArguments Concat(LuaArguments args)
        {
            list.AddRange(args.list);
            return this;
        }

        public IEnumerator<LuaObject> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public static implicit operator LuaArguments(LuaObject[] array)
        {
            return new LuaArguments(array);
        }

        public static implicit operator LuaObject[] (LuaArguments args)
        {
            return args.list.ToArray();
        }

        public LuaArguments GetSubset(int startIndex)
        {
            if (startIndex >= list.Count)
            {
                return new LuaArguments(new LuaObject[] { });
            }
            else
            {
                return new LuaArguments(list.GetRange(startIndex, list.Count - startIndex));
            }
        }
    }

    // http://www.lua.org/pil/2.html
    /// <summary>
    /// A Lua type
    /// </summary>
    public enum LuaType : byte
    {
        nil = 0,
        boolean,
        number,
        @string,
        userdata,
        function,
        thread,
        table
    }

    /// <summary>
    /// A Lua object. Can be any of the standard Lua objects
    /// </summary>
    public class LuaObject : DynamicObject, 
        IEnumerable<LuaTableItem>,
        IEquatable<LuaObject>,
        IComparable
    {
        internal object _luaObj;
        private LuaType type;
        private LuaObject _metaTable = Nil;

        public LuaObject()
        {
            this._metaTable = Nil;
        }

        private LuaObject(object Obj, LuaType Type)
        {
            this._metaTable = Nil;
            this._luaObj = Obj;
            this.type = Type;
        }

        ~LuaObject()
        {
            var handler = GetMetaMethod(LuaEvents.GC);
            if (handler.IsFunction)
                handler.AsFunction()(new LuaObject[] { this });
        }

        #region Common objects
        /// <summary>
        /// An empty/unset value
        /// </summary>
        public static readonly LuaObject Nil = new LuaObject() { _luaObj = null, type = LuaType.nil };

        /// <summary>
        /// A standard true boolean value
        /// </summary>
        public static readonly LuaObject True = new LuaObject { _luaObj = true, type = LuaType.boolean };

        /// <summary>
        /// A standard false boolean value
        /// </summary>
        public static readonly LuaObject False = new LuaObject { _luaObj = false, type = LuaType.boolean };

        /// <summary>
        /// Zero (number)
        /// </summary>
        public static readonly LuaObject Zero = new LuaObject { _luaObj = 0d, type = LuaType.number };

        /// <summary>
        /// And empty string
        /// </summary>
        public static readonly LuaObject EmptyString = new LuaObject { _luaObj = "", type = LuaType.@string };
        #endregion

        /// <summary>
        /// Gets the underlying Lua type
        /// </summary>
        public LuaType Type { get { return type; } internal set { type = value; } }

        /// <summary>
        /// Checks whether the type matches or not
        /// </summary>
        public bool Is(LuaType type)
        {
            return this.type == type;
        }

        /// <summary>
        /// Creates a Lua object from a .NET object
        /// Automatically checks if there is a matching Lua type.
        /// If not, creates a userdata value
        /// </summary>
        public static LuaObject FromObject(object obj)
        {
            if (obj == null) return Nil;
            {if (obj is LuaObject v) return v;}
            {if (obj is bool v) return FromBool(v);}
            {if (obj is string v) return FromString(v);}
            {if (obj is LuaFunction v) return FromFunction(v);}
            {if (obj is ILuaTable v) return FromTable(v);}
            {if (obj is double v) return FromNumber(v);}
            {if (obj is float v) return FromNumber(v);}
            {if (obj is int v) return FromNumber(v);}
            {if (obj is uint v) return FromNumber(v);}
            {if (obj is short v) return FromNumber(v);}
            {if (obj is ushort v) return FromNumber(v);}
            {if (obj is long v) return FromNumber(v);}
            {if (obj is ulong v) return FromNumber(v);}
            {if (obj is byte v) return FromNumber(v);}
            {if (obj is sbyte v) return FromNumber(v);}
            {if (obj is Thread) return new LuaObject { _luaObj = obj, type = LuaType.thread };}
            return FromUserData(obj);
        }

        #region Boolean
        /// <summary>
        /// Creates a Lua object from a boolean value
        /// </summary>
        public static LuaObject FromBool(bool bln)
        {
            if (bln)
                return True;

            return False;
        }

        public static implicit operator LuaObject(bool bln)
        {
            return FromBool(bln);
        }

        public static implicit operator bool(LuaObject obj)
        {
            return obj.AsBool();
        }

        public static bool operator true(LuaObject obj)
        {
            return obj.AsBool();
        }

        public static bool operator false(LuaObject obj)
        {
            return !obj.AsBool();
        }

        /// <summary>
        /// Gets whether this is a boolean object
        /// </summary>
        public bool IsBool { get { return type == LuaType.boolean; } }

        /// <summary>
        /// Converts this Lua object into a boolean
        /// </summary>
        /// <returns></returns>
        public bool AsBool()
        {
            if (_luaObj == null)
                return false;

            if (_luaObj is bool && ((bool)_luaObj) == false)
                return false;

            return true;
        }
        #endregion

        #region Number
        /// <summary>
        /// Creates a Lua object from a double
        /// </summary>
        public static LuaObject FromNumber(double number)
        {
            if (number == 0d)
                return Zero;

            return new LuaObject(number, LuaType.number);
        }

        public static LuaObject FromNumber(long number)
        {
            return new LuaObject(number, LuaType.number);
        }

        public static implicit operator LuaObject(double number)
        {
            return FromNumber(number);
        }

        public static implicit operator double(LuaObject obj)
        {
            return obj.AsNumber();
        }

        /// <summary>
        /// Gets whether this is a number object
        /// </summary>
        public bool IsNumber { get { return type == LuaType.number; } }

        /// <summary>
        /// Converts this object into a number
        /// </summary>
        public double AsNumber()
        {
            return (double)Convert.ChangeType(_luaObj, typeof(double));
        }

        public long AsLongNumber()
        {
            return (long)Convert.ChangeType(_luaObj, typeof(long));
        }

        public bool TryConvertToNumber(out double value)
        {
            if (IsNumber)
            {
                value = AsNumber();
                return true;
            }
            else if (IsString)
            {
                if (double.TryParse(AsString(), out value))
                    return true;
                else
                    return false;
            }
            else
            {
                value = 0d;
                return false;
            }
        }

        public bool TryConvertToLong(out long value)
        {
            if (IsNumber)
            {
                if (_luaObj is long lValue)
                {
                    value = lValue;
                    return true;
                }

                var dValue = AsNumber();
                value = (int)dValue;
                if (dValue - value < double.Epsilon)
                {
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public bool TryConvertToInt(out int value)
        {
            var result = TryConvertToLong(out long longValue);
            value = (int)longValue;
            return result;
        }

        public long AsInt()
        {
            if (TryConvertToLong(out long value))
            {
                return value;
            }
            throw new LuaException($"Cannot convert to integer number. Type: {Type}, Value: {_luaObj}");
        }
        #endregion

        #region String
        /// <summary>
        /// Creates a Lua object from a string
        /// </summary>
        public static LuaObject FromString(string str)
        {
            if (str == null)
                return Nil;

            if (str.Length == 0)
                return EmptyString;

            return new LuaObject(str, LuaType.@string);
        }

        public static implicit operator LuaObject(string str)
        {
            return FromString(str);
        }

        public static implicit operator string(LuaObject obj)
        {
            return obj.AsString();
        }

        public bool IsString { get { return type == LuaType.@string; } }

        public string AsString()
        {
            return _luaObj?.ToString();
        }

        public bool TryConvertToString(out string value)
        {
            if (IsString || IsNumber)
            {
                value = AsString();
                return true;
            }
            value = null;
            return false;
        }
        #endregion

        #region Function
        /// <summary>
        /// Creates a Lua object from a Lua function
        /// </summary>
        public static LuaObject FromFunction(LuaFunction fn)
        {
            if (fn == null)
                return Nil;

            return new LuaObject(fn, LuaType.function);
        }

        public static implicit operator LuaObject(LuaFunction fn)
        {
            return FromFunction(fn);
        }

        public bool IsFunction { get { return type == LuaType.function; } }

        public LuaFunction AsFunction()
        {
            var fn = _luaObj as LuaFunction;
            if (fn == null)
                throw new LuaException("cannot call non-function");

            return fn;
        }
        #endregion

        #region Table
        /// <summary>
        /// Creates a Lua object from a Lua table
        /// </summary>
        public static LuaObject FromTable(ILuaTable table)
        {
            if (table == null)
                return Nil;

            return new LuaObject(table, LuaType.table);
        }

        /// <summary>
        /// Creates and initializes a Lua object with a table value
        /// </summary>
        /// <param name="initItems">The initial items of the table to create</param>
        public static LuaObject NewTable()
        {
            var table = FromTable(new LuaTable());
            return table;
        }

        public bool IsTable { get { return type == LuaType.table; } }

        public ILuaTable AsTable()
        {
            return _luaObj as ILuaTable;
        }
        #endregion

        #region UserData
        /// <summary>
        /// Creates a Lua object from a .NET object
        /// </summary>
        public static LuaObject FromUserData(object userData)
        {
            if (userData == null)
                return Nil;

            //return new LuaObject { _luaObj = userData, type = LuaType.userdata };
            return new LuaObject(userData, LuaType.userdata);
        }

        /// <summary>
        /// Gets whether this object is nil
        /// </summary>
        public bool IsNil { get { return type == LuaType.nil; } }

        /// <summary>
        /// Gets whether this object is userdata
        /// </summary>
        public bool IsUserData { get { return type == LuaType.userdata; } }

        /// <summary>
        /// Returns the CLI object underneath the wrapper
        /// </summary>
        public object AsUserData()
        {
            return _luaObj;
        } 
        #endregion

        public static LuaObject GetBinHandler(LuaObject a, LuaObject b, string f)
        {
            var f1 = a.GetMetaMethod(f);
            if (!f1.IsNil)
            {
                return f1;
            }

            var f2 = b.GetMetaMethod(f);
            return f2;
        }

        public LuaObject Len()
        {
            if (IsString)
                return AsString().Length;
            else
            {
                var handler = GetMetaMethod(LuaEvents.LEN);
                if (!handler.IsNil)
                    return handler.Call(this)[0];
                else if (IsTable)
                    return AsTable().List.Count;
                else
                    throw new LuaException("Invalid op");
            }
        }

        public LuaObject Concat(LuaObject op)
        {
            if ((IsString || IsNumber) && (op.IsString || op.IsNumber))
                return string.Concat(this, op);
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.CONCAT);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
                else
                    throw new LuaException("Invalid op");
            }
        }

        public LuaObject Not()
        {
            if (!AsBool())
            {
                return True;
            }
            else
            {
                return False;
            }
        }

        public static LuaObject operator !(LuaObject a)
        {
            return a.Not();
        }

        public LuaObject Add(LuaObject op)
        {
            if (TryConvertToNumber(out var a) && op.TryConvertToNumber(out var b))
                return FromNumber(a + b);
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.ADD);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator +(LuaObject op1, LuaObject op2)
        {
            return op1.Add(op2);
        }

        public LuaObject Subtract(LuaObject op)
        {
            if (TryConvertToNumber(out var a) && op.TryConvertToNumber(out var b))
                return FromNumber(a - b);
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.SUB);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator -(LuaObject op1, LuaObject op2)
        {
            return op1.Subtract(op2);
        }

        public LuaObject Multiply(LuaObject op)
        {
            if (TryConvertToNumber(out var a) && op.TryConvertToNumber(out var b))
                return FromNumber(a * b);
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.MUL);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator *(LuaObject op1, LuaObject op2)
        {
            return op1.Multiply(op2);
        }

        public LuaObject Divide(LuaObject op)
        {
            if (TryConvertToNumber(out var a) && op.TryConvertToNumber(out var b))
                return FromNumber(a / b);
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.DIV);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator /(LuaObject op1, LuaObject op2)
        {
            return op1.Divide(op2);
        }

        public LuaObject Modulo(LuaObject op)
        {
            if (TryConvertToNumber(out var a) && op.TryConvertToNumber(out var b))
                return FromNumber(a - Math.Floor(a / b) * b);
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.MOD);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator %(LuaObject op1, LuaObject op2)
        {
            return op1.Modulo(op2);
        }

        public LuaObject Pow(LuaObject op)
        {
            if (TryConvertToNumber(out var a) && op.TryConvertToNumber(out var b))
                return FromNumber(Math.Pow(a, b));
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.POW);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        // LuaEvents.pow
        //public static LuaObject operator **(LuaObject a, LuaObject b)
        //{
        //    return LuaEvents.pow_event(a, b);
        //}
        public LuaObject Negate()
        {
            if (TryConvertToNumber(out var a))
            {
                return FromNumber(-a);
            }
            else
            {
                var handler = GetMetaMethod(LuaEvents.UNM);
                if (!handler.IsNil)
                    return handler.Call(this)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator -(LuaObject op)
        {
            return op.Negate();
        }

        private bool BitwiseAnd(LuaObject op, out LuaObject result)
        {
            if (TryConvertToLong(out long value1) &&
                op.TryConvertToLong(out long value2))
            {
                result = FromNumber(value1 & value2);
                return true;
            }
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.BITWISE_AND);
                if (!handler.IsNil)
                {
                    result = handler.Call(this, op)[0];
                    return true;
                }
            }

            result = Nil;
            return false;
        }

        public LuaObject BitwiseAnd(LuaObject op)
        {
            if (BitwiseAnd(op, out var result))
            {
                return result;
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator &(LuaObject a, LuaObject b)
        {
            a ??= Nil;
            b ??= Nil;
            if (a.BitwiseAnd(b, out var result))
            {
                return result;
            }

            if (a.IsNil || !a.AsBool())
                return a;
            else
            {
                return b;
            }
        }

        private bool BitwiseOr(LuaObject op, out LuaObject result)
        {
            if (TryConvertToLong(out long value1) &&
                op.TryConvertToLong(out long value2))
            {
                result = FromNumber(value1 | value2);
                return true;
            }
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.BITWISE_OR);
                if (!handler.IsNil)
                {
                    result = handler.Call(this, op)[0];
                    return true;
                }
            }

            result = Nil;
            return false;
        }

        public LuaObject BitwiseOr(LuaObject op)
        {
            if (BitwiseOr(op, out var result))
            {
                return result;
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator |(LuaObject a, LuaObject b)
        {
            a ??= Nil;
            b ??= Nil;
            if (a.BitwiseOr(b, out var result))
            {
                return result;
            }

            if (a.IsNil || !a.AsBool())
            {
                return b;
            }
            else
                return a;
        }

        public LuaObject BitwiseXor(LuaObject op)
        {
            if (TryConvertToLong(out long value1) &&
                op.TryConvertToLong(out long value2))
            {
                return FromNumber(value1 ^ value2);
            }
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.BITWISE_XOR);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator ^(LuaObject a, LuaObject b)
        {
            return a.BitwiseXor(b);
        }

        public LuaObject BitwiseNot()
        {
            if (TryConvertToLong(out long value))
            {
                return FromNumber(~value);
            }
            else
            {
                var handler = GetMetaMethod(LuaEvents.BITWISE_NOT);
                if (!handler.IsNil)
                    return handler.Call()[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public static LuaObject operator ~(LuaObject a)
        {
            return a.BitwiseNot();
        }

        public LuaObject ShiftLeft(LuaObject op)
        {
            if (TryConvertToLong(out long value1) &&
                op.TryConvertToInt(out int value2))
            {
                return FromNumber(value1 << value2);
            }
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.SHIFT_LEFT);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        public LuaObject ShiftRight(LuaObject op)
        {
            if (TryConvertToLong(out long value1) &&
                op.TryConvertToInt(out int value2))
            {
                return FromNumber(value1 >> value2);
            }
            else
            {
                var handler = GetBinHandler(this, op, LuaEvents.SHIFT_RIGHT);
                if (!handler.IsNil)
                    return handler.Call(this, op)[0];
            }

            throw new LuaException("Invalid arithmetic operation");
        }

        //public static LuaObject operator <<(LuaObject a, int b)
        //{
        //    return LuaEvents.shl_event(a, b);
        //}

        //public static LuaObject operator >>(LuaObject a, int b)
        //{
        //    return LuaEvents.shr_event(a, b);
        //}

        public int CompareTo(object obj)
        {
            var luaObj = FromObject(obj);
            if (LuaEquals(luaObj))
            {
                return 0;
            }
            return this < luaObj ? -1 : 1;
        }

        public static bool operator <(LuaObject op1, LuaObject op2)
        {
            if (op1.IsNumber && op2.IsNumber)
                return op1.AsNumber() < op2.AsNumber();
            else if (op1.IsString && op2.IsString)
            {
                int n = StringComparer.CurrentCulture.Compare(op1.AsString(), op2.AsString());
                return (n < 0);
            }
            else
            {
                var handler = GetBinHandler(op1, op2, LuaEvents.LESS);
                if (!handler.IsNil)
                    return handler.Call(op1, op2)[0];
                else
                    throw new ArgumentException("attempt to compare " + op1.type.ToString() + " with " + op2.type.ToString());
            }
        }

        public static bool operator >(LuaObject op1, LuaObject op2)
        {
            return !(op1 <= op2);
        }

        public static bool operator <=(LuaObject op1, LuaObject op2)
        {
            if (op1.IsNumber && op2.IsNumber)
                return op1.AsNumber() <= op2.AsNumber();
            else if (op1.IsString && op2.IsString)
            {
                int n = StringComparer.CurrentCulture.Compare(op1.AsString(), op2.AsString());
                return (n <= 0);
            }
            else
            {
                var handler = GetBinHandler(op1, op2, LuaEvents.LESS_OR_EQUAL);
                if (!handler.IsNil)
                    return handler.Call(op1, op2)[0];
                else
                    return op1.LuaEquals(op2) || op1 < op2;
            }
        }

        public static bool operator >=(LuaObject a, LuaObject b)
        {
            return !(a < b);
        }

        public static bool operator ==(LuaObject a, object b)
        {
            a ??= Nil;
            return a.Equals(b);
        }

        public static bool operator !=(LuaObject a, object b)
        {
            return !(a == b);
        }

        public IEnumerator<LuaTableItem> GetEnumerator()
        {
            if (_luaObj is IEnumerable<LuaTableItem> table)
            {
                return table.GetEnumerator();
            }

            throw new LuaException($"[{Type}] LuaObject cannot iterate");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public LuaObject this[LuaObject key]
        {
            get
            {
                LuaObject handler;

                if (IsTable)
                {
                    var v = BasicLibrary.RawGet(this, key);
                    if (!v.IsNil)
                        return v;
                    else
                    {
                        handler = GetMetaMethod(LuaEvents.INDEX);
                        if (handler.IsNil)
                            return Nil;
                    }
                }
                else
                {
                    handler = GetMetaMethod(LuaEvents.INDEX);
                    if (handler.IsNil)
                        throw new LuaException("Invalid argument");
                }

                if (handler.IsFunction)
                    return handler.AsFunction()(new LuaObject[] { this, key })[0];
                else if (!handler.IsNil)
                    return handler[key];
                else
                    return Nil;
            }
            set
            {
                LuaObject handler;
                if (IsTable)
                {
                    var v = BasicLibrary.RawGet(this, key);
                    if (!v.IsNil)
                    {
                        BasicLibrary.RawSet(this, key, value);
                        return;
                    }
                    else
                    {
                        handler = GetMetaMethod(LuaEvents.NEW_INDEX);
                        if (handler.IsNil)
                        {
                            BasicLibrary.RawSet(this, key, value);
                            return;
                        }
                    }
                }
                else
                {
                    handler = GetMetaMethod(LuaEvents.NEW_INDEX);
                    if (handler.IsNil)
                        throw new LuaException("Invalid op");
                }

                if (handler.IsFunction)
                    handler.AsFunction()(new LuaObject[] { this, key, value });
                else
                    handler[key] = value;
            }
        }

        public LuaObject GetOrSet(string key, Func<LuaObject> creator)
        {
            var result = this[key];
            if (result.IsNil)
            {
                result = creator();
                this[key] = result;
            }
            return result;
        }

        // Unlike AsString, this will return string representations of nil, tables, and functions
        public override string ToString()
        {
            var toStringMethod = GetMetaMethod(LuaEvents.TO_STRING);
            if (toStringMethod != Nil)
            {
                return toStringMethod.Call(this)[0].AsString();
            }

            var nameField = GetMetaMethod(LuaEvents.NAME);
            if (nameField != Nil)
            {
                var name = nameField.ToString();
                if (IsTable)
                {
                    return $"{name}: {RuntimeHelpers.GetHashCode(this)}";
                }
                else
                {
                    return name;
                }
            }
            
            if (IsNil)
            {
                return "nil";
            }
            if (IsTable)
            {
                return $"table: {RuntimeHelpers.GetHashCode(this)}";
            }
            if (IsFunction)
            {
                return $"function: {RuntimeHelpers.GetHashCode(this)}";
            }
            if (IsBool)
            {
                return _luaObj.ToString().ToLower();
            }
            return _luaObj.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is LuaObject otherLuaObj)
                return Equals(otherLuaObj);
            else
                return Equals(_luaObj, obj);
        }

        public bool Equals(LuaObject other)
        {
            if (_luaObj == null)
            {
                return other._luaObj == null;
            }
            return _luaObj.Equals(other._luaObj);
        }

        public bool LuaEquals(LuaObject other)
        {
            static LuaObject GetEqualHandler(LuaObject a, LuaObject b)
            {
                if ((a.Type != b.Type) || (a.IsTable && b.IsUserData))
                    return Nil;
                var mm1 = a.GetMetaMethod(LuaEvents.EQUAL);
                var mm2 = b.GetMetaMethod(LuaEvents.EQUAL);
                if (mm1 == mm2)
                    return mm1;
                else
                    return Nil;
            }

            if (this == other)
                return true;
            var handler = GetEqualHandler(this, other);
            if (!handler.IsNil)
                return handler.Call(this, other)[0];
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_luaObj != null ? _luaObj.GetHashCode() : 0) ^ (byte)type;
            }
        }

        /// <summary>
        /// Calls the object passing the instance as first argument. Uses the metaField __call
        /// </summary>
        /// <param name="instance">The object to be passed as first argument</param>
        /// <param name="args">Arguments to be passed after the object</param>
        public LuaArguments MethodCall(LuaObject instance, LuaArguments args)
        {
            var objs = new LuaObject[args.Length + 1];
            objs[0] = instance;
            for (int i = 0; i < args.Length; i++)
            {
                objs[i + 1] = args[i];
            }

            return this.Call(objs);
        }

        /// <summary>
        /// Calls the object. If this is not a function, it calls the metaTable field __call
        /// </summary>
        /// <param name="args">The arguments to pass</param>
        public LuaArguments Call(params LuaObject[] args)
        {
            return this.Call(new LuaArguments(args));
        }

        /// <summary>
        /// Calls the object. If this is not a function, it calls the metaTable field __call
        /// </summary>
        /// <param name="args">The arguments to pass</param>
        public LuaArguments Call(LuaArguments args)
        {
            if (IsFunction)
                return AsFunction()(args);
            else
            {
                var handler = GetMetaMethod(LuaEvents.CALL);
                if (handler.IsFunction)
                {
                    return handler.AsFunction()(args.Prepend(this).ToArray());
                }
                else
                    throw new LuaException("Cannot call non function");
            }
        }

        public LuaObject GetMetaTable()
        {
            var metaTable = _metaTable ?? Nil;
            if (metaTable.Type != LuaType.table)
            {
                return metaTable;
            }

            var __metaTable = metaTable[LuaEvents.META_TABLE];
            if (__metaTable != null)
            {
                metaTable = __metaTable;
            }
            return metaTable;
        }

        public LuaObject SetMetaTable(LuaObject metaTable, bool force)
        {
            if (force)
            {
                _metaTable = metaTable;
                return this;
            }

            var oldMetaTable = _metaTable;
            if (oldMetaTable.IsTable && oldMetaTable.AsTable().ContainsKey(LuaEvents.META_TABLE))
            {
                throw new LuaException("cannot change a protected metaTable");
            }
            _metaTable = metaTable;
            return this;
        }

        public LuaObject GetMetaMethod(string e)
        {
            var metaTable = GetMetaTable();
            if (metaTable.IsNil)
            {
                return Nil;
            }
            return metaTable[e];
        }

        #region DynamicObject

        /// <summary>
        /// Gets a standard .NET value from LuaObject
        /// </summary>
        /// <returns>The LuaObject is <paramref name="a"/> is a function or a table, its underlying _luaObj if not</returns>
        internal static object GetObject(LuaObject a)
        {
            if (a.Type != LuaType.table && a.Type != LuaType.function)
                return a._luaObj;
            else
                return a;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            LuaObject obj = this[binder.Name];
            if (obj.IsNil)
                return false;
            else
            {
                result = GetObject(obj);
                return true;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (value is LuaObject v)
                this[binder.Name] = v;
            else
                this[binder.Name] = FromObject(value);
            return true;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            LuaObject[] passingArgs = Array.ConvertAll(args, FromObject);
            LuaArguments ret = Call(passingArgs);
            if (ret.Length == 1)
            {
                if (ret[0].IsNil)
                    result = null;
                else
                    result = GetObject(ret[0]);
                return true;
            }
            else
            {
                object[] res = Array.ConvertAll(ret.ToArray(), x => GetObject(x));
                result = res;
                return true;
            }
        }
        #endregion
    }
}
