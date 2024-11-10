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
using System.IO;

namespace NetLua
{
    /// <summary>
    /// Holds a scope and its variables
    /// </summary>
    public class LuaContext : DynamicObject
    {
        const string _ENV = "_ENV";

        string _executionDir = null;
        readonly Func<Parser> _parserGetter;
        readonly LuaContext _parent;
        readonly IDictionary<string, LuaObject> _variables;
        LuaArguments _varArgs;
        internal LuaArguments Varargs
        {
            get => _varArgs;
            set => _varArgs = value;
        }

        public LuaContext(LuaContext parent, LuaObject env, Func<Parser> parserGetter)
        {
            _parserGetter = parserGetter ?? parent?._parserGetter;
            _parent = parent;
            _variables = new Dictionary<string, LuaObject>();
            _varArgs = new LuaArguments();

            if (_parent == null)
            {
                if (env == null)
                {
                    env = LuaObject.NewTable();
                }
                env["_G"] = env;
                env["_VERSION"] = BasicLibrary._VERSION;
                _variables[_ENV] = env;
            }
            else
            {
                if (env == null)
                {
                    env = _parent._variables[_ENV];
                }
                _variables[_ENV] = env;
            }
        }

        public LuaContext(LuaContext parent, LuaObject env) : this(parent, env, null)
        {
        }

        /// <summary>
        /// Used to create scopes
        /// </summary>
        /// <param name="parent"></param>
        public LuaContext(LuaContext parent) : this(parent, null, null) {}

        /// <summary>
        /// Creates a base context
        /// </summary>
        public LuaContext() : this(null) { }

        public bool TryGetExecutionDir(out string executionDir)
        {
            executionDir = _executionDir;
            if (executionDir != null)
            {
                return true; 
            }

            if (_parent != null && _parent.TryGetExecutionDir(out executionDir))
            {
                return true;
            }

            return false;
        }

        public string GetFullPath(string filename, bool setExecutionDir)
        {
            if (TryGetExecutionDir(out var executionDir))
            {
                filename = Path.Combine(executionDir, filename);
            }
            else
            {
                filename = Path.GetFullPath(filename);
                if (setExecutionDir)
                {
                    executionDir = Path.GetDirectoryName(filename);
                    var current = this;
                    while (current != null)
                    {
                        current._executionDir = executionDir;
                        current = current._parent;
                    }
                }
            }
            return filename;
        }

        /// <summary>
        /// Sets or creates a variable in the local scope
        /// </summary>
        public void SetLocal(string Name, LuaObject Value)
        {
            _variables[Name] = Value ?? LuaObject.Nil;
        }

        /// <summary>
        /// Sets or creates a variable in the global scope
        /// </summary>
        public void SetGlobal(string Name, LuaObject Value)
        {
            _variables[_ENV][Name] = Value ?? LuaObject.Nil;
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

            if (_variables.TryGetValue(_ENV, out var env))
            {
                return env[name];
            }
            return LuaObject.Nil;
        }

        public LuaObject GetOrSet(string name, Func<LuaObject> creator)
        {
            var result = Get(name);
            if (result.IsNil)
            {
                result = creator();
                Set(name, result);
            }
            return result;
        }

        public bool TryGetLocal(string name, out LuaObject value)
        {
            var current = this;
            while (current != null)
            {
                if (current._variables.TryGetValue(name, out var obj))
                {
                    value = obj ?? LuaObject.Nil;
                    return true;
                }
                current = current._parent;
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
                if (current._variables.ContainsKey(name))
                {
                    current.SetLocal(name, value);
                    return;
                }
                current = current._parent;
            }

            SetGlobal(name, value);
        }

        /// <summary>
        /// Parses and executes the specified file
        /// </summary>
        /// <param name="filename">The file to execute</param>
        public LuaArguments DoFile(string filename)
        {
            filename = GetFullPath(filename, true);
            var source = File.ReadAllText(filename);
            return DoString(source, filename);
        }

        /// <summary>
        /// Parses and executes the specified string
        /// </summary>
        public LuaArguments DoString(string chunk, string chunkName = null)
        {
            var function = Load(chunk, chunkName);
            return function(Lua.Return());
        }

        /// <summary>
        /// Parses and executes the specified parsed block
        /// </summary>
        public LuaArguments DoAst(Block block)
        {
            FunctionDefinition def = new FunctionDefinition();
            def.Arguments = new List<Argument>();
            def.Body = block;
            var function = LuaCompiler.CompileFunction(def, Expression.Constant(this)).Compile();
            return function().Call(Lua.Return());
        }

        public LuaFunction Load(string chunk, string chunkName = null, LuaObject env = null)
        {
            var parser = _parserGetter?.Invoke() ?? new Parser();
            env ??= LuaObject.Nil;
            FunctionDefinition def = new FunctionDefinition();
            def.Arguments = new List<Argument>();
            def.Body = parser.ParseString(chunk, chunkName);
            var function = LuaCompiler.CompileFunction(def, Expression.Constant(this), Expression.Constant(env)).Compile();
            return function().AsFunction();
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
