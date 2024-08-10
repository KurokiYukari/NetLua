/*
 * See LICENSE file
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Dynamic;
using NetLua.Ast;
using System.Linq.Expressions;

namespace NetLua
{
    /// <summary>
    /// Holds a scope and its variables
    /// </summary>
    public class LuaContext : DynamicObject
    {
        const string _ENV = "_ENV";

        readonly LuaContext parent;
        readonly IDictionary<string, LuaObject> variables;
        LuaArguments varargs;

        /// <summary>
        /// Used to create scopes
        /// </summary>
        public LuaContext(LuaContext Parent)
        {
            parent = Parent;
            variables = new Dictionary<string, LuaObject>();
            varargs = new LuaArguments(new LuaObject[] { });

            if (parent == null)
            {
                var env = LuaObject.NewTable();
                env["_G"] = env;
                variables[_ENV] = env;
            }
            else
            {
                variables[_ENV] = parent.variables[_ENV];
            }
        }

        /// <summary>
        /// Creates a base context
        /// </summary>
        public LuaContext() : this(null) { }

        /// <summary>
        /// Sets or creates a variable in the local scope
        /// </summary>
        public void SetLocal(string Name, LuaObject Value)
        {
            variables[Name] = Value ?? LuaObject.Nil;
        }

        /// <summary>
        /// Sets or creates a variable in the global scope
        /// </summary>
        public void SetGlobal(string Name, LuaObject Value)
        {
            variables[_ENV][Name] = Value ?? LuaObject.Nil;
        }

        /// <summary>
        /// Returns the nearest declared variable value or nil
        /// </summary>
        public LuaObject Get(string name)
        {
            if (TryGetLocal(name, out var value))
            {
                return value;
            }

            if (variables.TryGetValue(_ENV, out var env))
            {
                return env[name];
            }
            return LuaObject.Nil;
        }

        public bool TryGetLocal(string name, out LuaObject value)
        {
            var current = this;
            while (current != null)
            {
                if (current.variables.TryGetValue(name, out var obj))
                {
                    value = obj ?? LuaObject.Nil;
                    return true;
                }
                current = current.parent;
            }

            value = LuaObject.Nil;
            return false;
        }

        /// <summary>
        /// Sets the nearest declared variable or creates a new one
        /// </summary>
        public void Set(string name, LuaObject value)
        {
            var current = this;
            while (current != null)
            {
                if (current.variables.ContainsKey(name))
                {
                    current.SetLocal(name, value);
                    return;
                }
                current = current.parent;
            }

            SetGlobal(name, value);
        }

        internal LuaArguments Varargs
        {
            get
            {
                return varargs;
            }

            set
            {
                varargs = value;
            }
        }

        #region DynamicObject
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = Get(binder.Name);
            if (result == LuaObject.Nil)
                return false;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Set(binder.Name, LuaObject.FromObject(value));
            return true;
        }
        #endregion
    }
}
