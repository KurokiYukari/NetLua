﻿/*
 * See LICENSE file
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

using NetLua.Ast;

namespace NetLua
{
    public static class LuaCompiler
    {
        static readonly Type LuaContext_Type = typeof(LuaContext);
        static readonly Type LuaArguments_Type = typeof(LuaArguments);
        static readonly Type LuaObject_Type = typeof(LuaObject);

        static readonly MethodInfo LuaContext_Get = LuaContext_Type.GetMethod("Get");
        static readonly MethodInfo LuaContext_SetLocal = LuaContext_Type.GetMethod("SetLocal");
        static readonly MethodInfo LuaContext_SetGlobal = LuaContext_Type.GetMethod("SetGlobal");
        static readonly MethodInfo LuaContext_Set = LuaContext_Type.GetMethod("Set");

        static readonly MethodInfo LuaArguments_Concat = LuaArguments_Type.GetMethod("Concat");
        static readonly MethodInfo LuaArguments_Add = LuaArguments_Type.GetMethod("Add");

        static readonly ConstructorInfo LuaContext_New_parent = LuaContext_Type.GetConstructor(new[] { typeof(LuaContext) });
        static readonly ConstructorInfo LuaContext_New_parent_env = LuaContext_Type.GetConstructor(new[] { typeof(LuaContext), typeof(LuaObject) });
        static readonly ConstructorInfo LuaArguments_New = LuaArguments_Type.GetConstructor(new[] { typeof(LuaObject[]) });
        static readonly ConstructorInfo LuaArguments_New_argList = LuaArguments_Type.GetConstructor(new[] { typeof(LuaArguments[]) });
        static readonly ConstructorInfo LuaArguments_New_void = LuaArguments_Type.GetConstructor(new Type[] { });

        static class Helpers
        {
            public static LuaObject Pow(LuaObject left, LuaObject right)
            {
                return left.Pow(right);
            }
            public static LuaObject BitwiseAnd(LuaObject left, LuaObject right)
            {
                return left.BitwiseAnd(right);
            }
            public static LuaObject BitwiseOr(LuaObject left, LuaObject right)
            {
                return left.BitwiseOr(right);
            }
            public static LuaObject ShiftLeft(LuaObject left, LuaObject right)
            {
                return left.ShiftLeft(right);
            }
            public static LuaObject ShiftRight(LuaObject left, LuaObject right)
            {
                return left.ShiftRight(right);
            }
            public static LuaObject Equal(LuaObject left, LuaObject right)
            {
                return left.LuaEquals(right);
            }
            public static LuaObject NotEqual(LuaObject left, LuaObject right)
            {
                return !left.LuaEquals(right);
            }
            public static LuaObject Concat(LuaObject left, LuaObject right)
            {
                return left.Concat(right);
            }

            public static LuaObject ToNumber(LuaObject target)
            {
                if (target.IsNumber)
                {
                    return target;
                }
                if (target.IsString && double.TryParse(target.AsString(), out var result))
                {
                    return result;
                }
                return LuaObject.Nil;
            }
        }

        static readonly MethodInfo Operators_pow = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.Pow).Method;
        static readonly MethodInfo Operators_band = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.BitwiseAnd).Method;
        static readonly MethodInfo Operators_bor = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.BitwiseOr).Method;
        static readonly MethodInfo Operators_shl = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.ShiftLeft).Method;
        static readonly MethodInfo Operators_shr = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.ShiftRight).Method;
        static readonly MethodInfo Operators_eq = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.Equal).Method;
        static readonly MethodInfo Operators_neq = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.NotEqual).Method;
        static readonly MethodInfo Operators_concat = ((Func<LuaObject, LuaObject, LuaObject>)Helpers.Concat).Method;
        static readonly MethodInfo Helpers_toNumber = ((Func<LuaObject, LuaObject>)Helpers.ToNumber).Method;

        static readonly MethodInfo LuaObject_Len = ((Func<LuaObject>)LuaObject.Nil.Len).Method;
        static readonly MethodInfo LuaObject_Call = ((Func<LuaArguments, LuaArguments>)LuaObject.Nil.Call).Method;
        static readonly MethodInfo LuaObject_AsBool = ((Func<bool>)LuaObject.Nil.AsBool).Method;
        static readonly MethodInfo LuaObject_FromDouble = ((Func<double, LuaObject>)LuaObject.FromNumber).Method;

        static readonly LuaArguments VoidArguments = new LuaArguments();

        #region Helpers
        static Expression ToNumber(Expression Expression)
        {
            return Expression.Call(Helpers_toNumber, Expression);
        }

        static Expression CreateLuaArguments(params Expression[] Expressions)
        {
            var array = Expression.NewArrayInit(typeof(LuaObject), Expressions);
            return Expression.New(LuaArguments_New, array);
        }

        static Expression GetFirstArgument(Expression Expression)
        {
            if (Expression.Type == typeof(LuaArguments))
                return Expression.Property(Expression, "Item", Expression.Constant(0));
            else
                return Expression.ArrayAccess(Expression, Expression.Constant(0));
        }

        static Expression GetFirstArgumentAsBool(Expression Expression)
        {
            Expression e = GetFirstArgument(Expression);
            return Expression.Call(e, LuaObject_AsBool);
        }

        static Expression GetAsBool(Expression Expression)
        {
            return Expression.Call(Expression, LuaObject_AsBool);
        }

        static Expression GetArgument(Expression Expression, int n)
        {
            if (Expression.Type == typeof(LuaArguments))
                return Expression.Property(Expression, "Item", Expression.Constant(n));
            else
                return Expression.ArrayAccess(Expression, Expression.Constant(n));
        }
        #endregion

        #region Expressions
        static Expression CompileBinaryExpression(Ast.BinaryExpression expr, Expression Context)
        {
            var left = CompileSingleExpression(expr.Left, Context);
            var right = CompileSingleExpression(expr.Right, Context);
            switch (expr.Operation)
            {
                case BinaryOp.Addition:
                    return Expression.Add(left, right);
                case BinaryOp.And:
                    return Expression.AndAlso(left, right);
                case BinaryOp.Concat:
                    return Expression.Call(left, Operators_concat, right);
                case BinaryOp.Different:
                    return Expression.NotEqual(left, right, true, Operators_neq);
                case BinaryOp.Division:
                    return Expression.Divide(left, right);
                case BinaryOp.Equal:
                    return Expression.Equal(left, right, true, Operators_eq);
                case BinaryOp.GreaterOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);
                case BinaryOp.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case BinaryOp.LessOrEqual:
                    return Expression.LessThanOrEqual(left, right);
                case BinaryOp.LessThan:
                    return Expression.LessThan(left, right);
                case BinaryOp.Modulo:
                    return Expression.Modulo(left, right);
                case BinaryOp.Multiplication:
                    return Expression.Multiply(left, right);
                case BinaryOp.Or:
                    return Expression.OrElse(left, right);
                case BinaryOp.Power:
                    return Expression.Power(left, right, Operators_pow);
                case BinaryOp.Subtraction:
                    return Expression.Subtract(left, right);
                case BinaryOp.BitwiseLeftShift:
                    return Expression.LeftShift(left, right, Operators_shl);
                case BinaryOp.BitwiseRightShift:
                    return Expression.RightShift(left, right, Operators_shr);
                case BinaryOp.BitwiseAnd:
                    return Expression.And(left, right, Operators_band);
                case BinaryOp.BitwiseExclusiveOr:
                    return Expression.ExclusiveOr(left, right);
                case BinaryOp.BitwiseOr:
                    return Expression.Or(left, right, Operators_bor);
                default:
                    throw new NotImplementedException();
            }
        }

        static Expression CompileUnaryExpression(Ast.UnaryExpression expr, Expression Context)
        {
            var e = CompileSingleExpression(expr.Expression, Context);
            switch (expr.Operation)
            {
                case UnaryOp.Invert:
                    return Expression.Negate(e);
                case UnaryOp.Negate:
                    return Expression.Not(e);
                case UnaryOp.Length:
                    return Expression.Call(e, LuaObject_Len);
                case UnaryOp.BitwiseNot:
                    return Expression.OnesComplement(e);
                default:
                    throw new NotImplementedException();
            }
        }

        static Expression GetVariable(Ast.Variable expr, Expression Context)
        {
            if (expr.Prefix == null)
                return Expression.Call(Context, LuaContext_Get, Expression.Constant(expr.Name));
            else
            {
                Expression p = CompileSingleExpression(expr.Prefix, Context);
                return Expression.Property(p, "Item", Expression.Convert(Expression.Constant(expr.Name), LuaObject_Type));
            }
        }

        static Expression GetTableAccess(Ast.TableAccess expr, Expression Context)
        {
            var e = CompileSingleExpression(expr.Expression, Context);
            var i = CompileSingleExpression(expr.Index, Context);

            return Expression.Property(e, "Item", i);
        }

        // This function returns a Expression with type LuaArguments. Similar to CompileSingleExpression
        static Expression CompileExpression(IExpression expr, Expression Context)
        {
            if (expr is NumberLiteral)
                return CreateLuaArguments(Expression.Constant((LuaObject)((expr as NumberLiteral).Value)));
            if (expr is StringLiteral)
                return CreateLuaArguments(Expression.Constant((LuaObject)((expr as StringLiteral).Value)));
            if (expr is BoolLiteral)
                return CreateLuaArguments(Expression.Constant((LuaObject)((expr as BoolLiteral).Value)));
            if (expr is NilLiteral)
                return CreateLuaArguments(Expression.Constant(LuaObject.Nil));

            if (expr is Ast.BinaryExpression)
                return CreateLuaArguments(CompileBinaryExpression(expr as Ast.BinaryExpression, Context));
            if (expr is Ast.UnaryExpression)
                return CreateLuaArguments(CompileUnaryExpression(expr as Ast.UnaryExpression, Context));
            if (expr is Ast.Variable)
                return CreateLuaArguments(GetVariable(expr as Ast.Variable, Context));
            if (expr is Ast.TableAccess)
                return CreateLuaArguments(GetTableAccess(expr as Ast.TableAccess, Context));
            if (expr is Ast.FunctionCall)
                return CompileFunctionCallExpr(expr as Ast.FunctionCall, Context);
            if (expr is Ast.FunctionDefinition)
                return CreateLuaArguments(CompileFunctionDef(expr as Ast.FunctionDefinition, Context));
            if (expr is Ast.VarargsLiteral)
                return CompileVarargs(Context);
            if (expr is Ast.TableConstructor)
                return CreateLuaArguments(CompileTableConstructor(expr as Ast.TableConstructor, Context));
            throw new NotImplementedException();
        }

        // This function returns a Expression with type LuaObject. Similar to CompileExpression
        static Expression CompileSingleExpression(IExpression expr, Expression Context)
        {
            if (expr is NumberLiteral)
                return (Expression.Constant((LuaObject)((expr as NumberLiteral).Value)));
            if (expr is StringLiteral)
                return (Expression.Constant((LuaObject)((expr as StringLiteral).Value)));
            if (expr is BoolLiteral)
                return (Expression.Constant((LuaObject)((expr as BoolLiteral).Value)));
            if (expr is NilLiteral)
                return (Expression.Constant(LuaObject.Nil));

            if (expr is Ast.BinaryExpression)
                return (CompileBinaryExpression(expr as Ast.BinaryExpression, Context));
            if (expr is Ast.UnaryExpression)
                return (CompileUnaryExpression(expr as Ast.UnaryExpression, Context));
            if (expr is Ast.Variable)
                return (GetVariable(expr as Ast.Variable, Context));
            if (expr is Ast.TableAccess)
                return (GetTableAccess(expr as Ast.TableAccess, Context));
            if (expr is Ast.FunctionCall)
                return GetFirstArgument(CompileFunctionCallExpr(expr as Ast.FunctionCall, Context));
            if (expr is Ast.FunctionDefinition)
                return (CompileFunctionDef(expr as Ast.FunctionDefinition, Context));
            if (expr is Ast.VarargsLiteral)
                return GetFirstArgument(CompileVarargs(Context));
            if (expr is Ast.TableConstructor)
                return (CompileTableConstructor(expr as Ast.TableConstructor, Context));
            throw new NotImplementedException();
        }

        static Expression CompileTableConstructor(Ast.TableConstructor table, Expression Context)
        {
            var values = new List<KeyValuePair<Expression, Expression>>();
            int i = 0;
            var exprs = new List<Expression>();
            var type = typeof(LuaTable);
            var add = type.GetMethod("Add", new[] { LuaObject_Type, LuaObject_Type });
            var variable = Expression.Parameter(type);
            var assign = Expression.Assign(variable, Expression.New(type.GetConstructor(new Type[] { })));
            exprs.Add(assign);
            foreach (KeyValuePair<IExpression, IExpression> kvpair in table.Values)
            {
                if (i == table.Values.Count - 1)
                {
                    var k = CompileSingleExpression(kvpair.Key, Context);
                    var v = CompileExpression(kvpair.Value, Context);
                    var singlev = GetFirstArgument(v);
                    var ifFalse = Expression.Call(variable, add, k, singlev);

                    var counter = Expression.Parameter(typeof(int));
                    var value = Expression.Parameter(LuaArguments_Type);
                    var @break = Expression.Label();
                    var breakLabel = Expression.Label(@break);
                    var assignValue = Expression.Assign(value, v);
                    var assignCounter = Expression.Assign(counter, Expression.Constant(0));
                    var incrementCounter = Expression.Assign(counter, Expression.Increment(counter));
                    var loopCondition = Expression.LessThan(counter, Expression.Property(v, "Length"));
                    var addValue = Expression.Call(variable, add,
                        Expression.Add(k,
                            Expression.Call(LuaObject_FromDouble,
                                Expression.Convert(counter, typeof(double) ) ) ),
                        Expression.Property(value, "Item", counter) );

                    var check = Expression.IfThenElse(loopCondition, Expression.Block(addValue, incrementCounter), Expression.Break(@break));
                    var loopBody = Expression.Loop(check);
                    var ifTrue = Expression.Block(new[] { counter, value }, assignCounter, assignValue, loopBody, breakLabel);

                    var condition = Expression.IsTrue(Expression.Property(k, "IsNumber"));
                    var ifblock = Expression.IfThenElse(condition, ifTrue, ifFalse);
                    exprs.Add(ifblock);
                }
                else
                {
                    var k = CompileSingleExpression(kvpair.Key, Context);
                    var v = CompileSingleExpression(kvpair.Value, Context);

                    exprs.Add(Expression.Call(variable, add, k, v));
                }
                i++;
            }
            exprs.Add(Expression.Call(LuaObject_Type.GetMethod("FromTable"), variable));
            var block = Expression.Block(new[] { variable }, exprs.ToArray());
            return Expression.Invoke(Expression.Lambda<Func<LuaObject>>(block));
        }

        static Expression CompileVarargs(Expression Context)
        {
            return Expression.Property(Context, "Varargs");
        }

        static Expression CompileFunctionDef(FunctionDefinition def, Expression Context)
        {
            return Expression.Invoke(CompileFunction(def, Context));
        }

        static Expression CompileFunctionCallExpr(Ast.FunctionCall expr, Expression Context)
        {
            var function = CompileSingleExpression(expr.Function, Context);
            var args = new List<Expression>();
            Expression lastArg = null;
            int i = 0;
            foreach (IExpression e in expr.Arguments)
            {
                if (i == expr.Arguments.Count - 1)
                    lastArg = CompileExpression(e, Context);
                else
                    args.Add(CompileSingleExpression(e, Context));
                i++;
            }
            var arg = Expression.NewArrayInit(LuaObject_Type, args.ToArray());
            var luaarg = Expression.New(LuaArguments_New, arg);

            if (lastArg == null)
                return Expression.Call(function, LuaObject_Call, luaarg);
            else
                return Expression.Call(function, LuaObject_Call, Expression.Call(luaarg, LuaArguments_Concat, lastArg));
        }

        public static Expression<Func<LuaObject>> CompileFunction(Ast.FunctionDefinition func, Expression parent)
        {
            return CompileFunctionWithContext(func, Expression.New(LuaContext_New_parent, parent));
        }

        public static Expression<Func<LuaObject>> CompileFunction(Ast.FunctionDefinition func, Expression parent, Expression env)
        {
            return CompileFunctionWithContext(func, Expression.New(LuaContext_New_parent_env, parent, env));
        }

        private static Expression<Func<LuaObject>> CompileFunctionWithContext(Ast.FunctionDefinition func, Expression newContextExpr)
        {
            var exprs = new List<Expression>();

            var args = Expression.Parameter(LuaArguments_Type, "args");
            var label = Expression.Label(LuaArguments_Type, "exit");
            var @break = Expression.Label("break");

            var scopeVar = Expression.Parameter(LuaContext_Type, "funcScope");
            var assignScope = Expression.Assign(scopeVar, newContextExpr);

            #region Arguments init
            var len = Expression.Property(args, "Length");

            Expression argLen;
            if (func.Arguments.Count != 0 && 
                func.Arguments[func.Arguments.Count - 1].Name == "...")
            {
                argLen = Expression.Constant(func.Arguments.Count - 1);
            }
            else
            {
                argLen = Expression.Constant(func.Arguments.Count);
            }

            var i = Expression.Parameter(typeof(int), "i");
            var assignI = Expression.Assign(i, Expression.Constant(0));
            var names = Expression.Parameter(typeof(string[]), "names");
            var assignNames = Expression.Assign(names, Expression.Constant(Array.ConvertAll<Argument, string>(func.Arguments.ToArray(), x => x.Name)));

            var innerCond = Expression.LessThan(i, argLen);
            var outerCond = Expression.LessThan(i, len);

            var innerIf = Expression.Call(scopeVar, LuaContext_SetLocal, Expression.ArrayAccess(names, i), Expression.Property(args, "Item", i));
            var varargs = Expression.Property(scopeVar, "Varargs");
            var innerElse = Expression.Call(varargs, LuaArguments_Add, Expression.Property(args, "Item", i));

            var outerIf = Expression.Block(Expression.IfThenElse(innerCond, innerIf, innerElse), Expression.Assign(i, Expression.Add(i, Expression.Constant(1))));
            var outerElse = Expression.Break(@break);

            var loopBody = Expression.IfThenElse(outerCond, outerIf, outerElse);
            var loop = Expression.Loop(loopBody);

            var breakLabel = Expression.Label(@break);
            #endregion

            var body = CompileBlock(func.Body, label, null, scopeVar);

            exprs.Add(assignScope);
            exprs.Add(assignI);
            exprs.Add(assignNames);
            exprs.Add(loop);
            exprs.Add(breakLabel);
            exprs.Add(body);
            exprs.Add(Expression.Label(label, Expression.Constant(VoidArguments)));

            var funcBody = Expression.Block(new[] { i, names, scopeVar }, exprs.ToArray());

            var function = Expression.Lambda<LuaFunction>(funcBody, args);
            var returnValue = Expression.Lambda<Func<LuaObject>>(Expression.Convert(function, LuaObject_Type), null);

            return returnValue;
        }
        #endregion

        #region Statements
        static Expression SetVariable(Ast.Variable expr, Expression value, Expression Context)
        {
            if (expr.Prefix == null)
                return Expression.Call(Context, LuaContext_Set, Expression.Constant(expr.Name), value);
            else
            {
                var prefix = CompileSingleExpression(expr.Prefix, Context);
                var index = Expression.Constant((LuaObject)(expr.Name));
                var set = Expression.Property(prefix, "Item", index);
                return Expression.Assign(set, value);
            }
        }

        static Expression CompileAssignment(Ast.Assignment assign, Expression Context)
        {
            var variable = Expression.Parameter(typeof(LuaArguments), "vars");
            var stats = new List<Expression>();

            stats.Add(Expression.Assign(variable, Expression.New(LuaArguments_New_void)));
            foreach (IExpression expr in assign.Expressions)
            {
                var ret = CompileExpression(expr, Context);
                stats.Add(Expression.Call(variable, LuaArguments_Concat, ret));
            }
            int i = 0;
            foreach (IAssignable var in assign.Variables)
            {
                var arg = GetArgument(variable, i);
                if (var is Ast.Variable)
                {
                    var x = var as Ast.Variable;
                    stats.Add(SetVariable(x, arg, Context));
                }
                else if (var is Ast.TableAccess)
                {
                    var x = var as Ast.TableAccess;

                    var expression = CompileSingleExpression(x.Expression, Context);
                    var index = CompileSingleExpression(x.Index, Context);

                    var set = Expression.Property(expression, "Item", index);
                    stats.Add(Expression.Assign(set, arg));
                }
                i++;
            }

            return Expression.Block(new[] { variable }, stats.ToArray());
        }

        static Expression CompileLocalAssignment(Ast.LocalAssignment assign, Expression Context)
        {
            var variable = Expression.Parameter(typeof(LuaArguments), "vars");
            var stats = new List<Expression>();

            stats.Add(Expression.Assign(variable, Expression.New(LuaArguments_New_void)));
            foreach (IExpression expr in assign.Values)
            {
                var ret = CompileExpression(expr, Context);
                stats.Add(Expression.Call(variable, LuaArguments_Concat, ret));
            }
            int i = 0;
            foreach (string var in assign.Names)
            {
                var arg = GetArgument(variable, i);
                stats.Add(Expression.Call(Context, LuaContext_SetLocal, Expression.Constant(var), arg));
                i++;
            }

            return Expression.Block(new[] { variable }, stats.ToArray());
        }

        static Expression CompileFunctionCallStat(Ast.FunctionCall call, Expression Context)
        {
            var variable = Expression.Parameter(typeof(LuaArguments), "vars");
            var stats = new List<Expression>();

            stats.Add(Expression.Assign(variable, Expression.New(LuaArguments_New_void)));

            var expression = CompileSingleExpression(call.Function, Context);
            int i = 0;

            foreach (Ast.IExpression arg in call.Arguments)
            {
                if (i == call.Arguments.Count - 1)
                {
                    var exp = CompileExpression(arg, Context);
                    stats.Add(Expression.Call(variable, LuaArguments_Concat, exp));
                }
                else
                {
                    var exp = CompileSingleExpression(arg, Context);
                    stats.Add(Expression.Call(variable, LuaArguments_Add, exp));
                }
                i++;
            }
            stats.Add(Expression.Call(expression, LuaObject_Call, variable));

            return Expression.Block(new[] { variable }, stats.ToArray());
        }

        static Expression CompileBlock(Ast.Block block, LabelTarget returnTarget, LabelTarget breakTarget, Expression Context)
        {
            var exprs = new List<Expression>();
            var scope = Expression.Parameter(LuaContext_Type);
            exprs.Add(Expression.Assign(scope, Context));

            foreach (IStatement s in block.Statements)
            {
                exprs.Add(CompileStatement(s, returnTarget, breakTarget, scope));
            }

            return Expression.Block(new[] { scope }, exprs.ToArray());
        }

        static Expression CompileWhileStat(Ast.WhileStat stat, LabelTarget returnTarget, Expression Context)
        {
            var cond = GetAsBool(CompileSingleExpression(stat.Condition, Context));
            var breakTarget = Expression.Label("break");
            var loopBody = CompileBlock(stat.Block, returnTarget, breakTarget, Expression.New(LuaContext_New_parent, Context));
            var condition = Expression.IfThenElse(cond, loopBody, Expression.Break(breakTarget));
            var loop = Expression.Loop(condition);

            return Expression.Block(loop, Expression.Label(breakTarget));
        }

        static Expression CompileIfStat(Ast.IfStat ifstat, LabelTarget returnTarget, LabelTarget breakTarget, Expression Context)
        {
            var condition = GetAsBool(CompileSingleExpression(ifstat.Condition, Context));
            var block = CompileBlock(ifstat.Block, returnTarget, breakTarget, Expression.New(LuaContext_New_parent, Context));

            if (ifstat.ElseIfs.Count == 0)
            {
                if (ifstat.ElseBlock == null)
                    return Expression.IfThen(condition, block);
                else
                {
                    var elseBlock = CompileBlock(ifstat.ElseBlock, returnTarget, breakTarget, Expression.New(LuaContext_New_parent, Context));
                    return Expression.IfThenElse(condition, block, elseBlock);
                }
            }
            else
            {
                Expression b = null;
                for (int i = ifstat.ElseIfs.Count - 1; i >= 0; i--)
                {
                    IfStat branch = ifstat.ElseIfs[i];
                    var cond = GetAsBool(CompileSingleExpression(branch.Condition, Context));
                    var body = CompileBlock(branch.Block, returnTarget, breakTarget, Expression.New(LuaContext_New_parent, Context));
                    if (b == null)
                    {
                        if (ifstat.ElseBlock == null)
                            b = Expression.IfThen(cond, body);
                        else
                        {
                            var elseBlock = CompileBlock(ifstat.ElseBlock, returnTarget, breakTarget, Expression.New(LuaContext_New_parent, Context));
                            b = Expression.IfThenElse(cond, body, elseBlock);
                        }
                    }
                    else
                        b = Expression.IfThenElse(cond, body, b);
                }

                var tree = Expression.IfThenElse(condition, block, b);
                return tree;
            }
        }

        static Expression CompileReturnStat(Ast.ReturnStat ret, LabelTarget returnTarget, Expression Context)
        {
            var variable = Expression.Parameter(LuaArguments_Type);
            var body = new List<Expression>();
            var ctor = Expression.New(LuaArguments_New_void);
            body.Add(Expression.Assign(variable, ctor));

            int i = 0;
            foreach (IExpression expr in ret.Expressions)
            {
                if (i == ret.Expressions.Count - 1)
                {
                    var exp = CompileExpression(expr, Context);
                    body.Add(Expression.Call(variable, LuaArguments_Concat, exp));
                }
                else
                {
                    var exp = CompileSingleExpression(expr, Context);
                    body.Add(Expression.Call(variable, LuaArguments_Add, exp));
                }
            }

            body.Add(Expression.Return(returnTarget, variable));

            return Expression.Block(new[] { variable }, body.ToArray());
        }

        static Expression CompileRepeatStatement(Ast.RepeatStat stat, LabelTarget returnTarget, Expression Context)
        {
            var ctx = Expression.New(LuaContext_New_parent, Context);
            var scope = Expression.Parameter(LuaContext_Type);
            var assignScope = Expression.Assign(scope, ctx);
            var @break = Expression.Label();

            var condition = GetAsBool(CompileSingleExpression(stat.Condition, scope));
            var body = CompileBlock(stat.Block, returnTarget, @break, scope);
            var check = Expression.IfThen(condition, Expression.Break(@break));
            var loop = Expression.Loop(Expression.Block(body, check));
            var block = Expression.Block(new[] { scope }, assignScope, loop, Expression.Label(@break));
            return block;
        }

        static Expression CompileNumericFor(Ast.NumericFor stat, LabelTarget returnTarget, Expression Context)
        {
            var varValue = ToNumber(CompileSingleExpression(stat.Var, Context));
            var limit = ToNumber(CompileSingleExpression(stat.Limit, Context));
            var step = ToNumber(CompileSingleExpression(stat.Step, Context));

            var var = Expression.Parameter(LuaObject_Type);
            var scope = Expression.Parameter(LuaContext_Type);
            var @break = Expression.Label();
            var assignScope = Expression.Assign(scope, Expression.New(LuaContext_New_parent, Context));
            var assignVar = Expression.Assign(var, varValue);

            var condition =
                Expression.Or(
                    Expression.And(
                        Expression.GreaterThan(step, Expression.Constant((LuaObject)0)),
                        Expression.LessThanOrEqual(var, limit)),
                    Expression.And(
                        Expression.LessThanOrEqual(step, Expression.Constant((LuaObject)0)),
                        Expression.GreaterThanOrEqual(var, limit)));
            var setLocalVar = Expression.Call(scope, LuaContext_SetLocal, Expression.Constant(stat.Variable), var);
            var innerBlock = CompileBlock(stat.Block, returnTarget, @break, scope);
            var sum = Expression.Assign(var, Expression.Add(var, step));
            var check = Expression.IfThenElse(GetAsBool(condition), Expression.Block(setLocalVar, innerBlock, sum), Expression.Break(@break));
            var loop = Expression.Loop(check);

            var body = Expression.Block(new[]{var, scope}, assignScope, assignVar, loop, Expression.Label(@break));

            return body;
        }

        static Expression CompileGenericFor(Ast.GenericFor stat, LabelTarget returnTarget, Expression Context)
        {
            var body = new List<Expression>();
            var args = Expression.Parameter(LuaArguments_Type);
            var f = GetArgument(args, 0);
            var s = GetArgument(args, 1);
            var var = GetArgument(args, 2);
            var fVar = Expression.Parameter(LuaObject_Type);
            var sVar = Expression.Parameter(LuaObject_Type);
            var varVar = Expression.Parameter(LuaObject_Type);
            var scope = Expression.Parameter(LuaContext_Type);

            var @break = Expression.Label();

            body.Add(Expression.Assign(args, Expression.New(LuaArguments_New_void)));
            foreach (IExpression expr in stat.Expressions)
            {
                body.Add(Expression.Call(args, LuaArguments_Concat,CompileExpression(expr, Context)));
            }
            body.Add(Expression.Assign(fVar, f));
            body.Add(Expression.Assign(sVar, s));
            body.Add(Expression.Assign(varVar, var));

            body.Add(Expression.Assign(scope, Expression.New(LuaContext_New_parent, Context)));

            var res = Expression.Parameter(LuaArguments_Type);
            var buildArgs = Expression.New(LuaArguments_New, Expression.NewArrayInit(typeof(LuaObject), sVar, varVar));
            var resAssign = Expression.Assign(res, Expression.Call(fVar, LuaObject_Call, buildArgs));
            List<Expression> exprs = new List<Expression>();
            exprs.Add(resAssign);
            for (int i = 0; i < stat.Variables.Count; i++)
            {
                var val = GetArgument(res, i);
                exprs.Add(Expression.Call(scope, LuaContext_SetLocal, Expression.Constant(stat.Variables[i]), val));
            }
            var check = Expression.IfThen(Expression.Property(GetArgument(res, 0), "IsNil"), Expression.Break(@break));
            exprs.Add(check);
            exprs.Add(Expression.Assign(varVar, GetFirstArgument(res)));
            exprs.Add(CompileBlock(stat.Block, returnTarget, @break, scope));

            var loopBody = Expression.Block(new[] { res }, exprs.ToArray());
            var loop = Expression.Loop(loopBody);
            body.Add(loop);
            body.Add(Expression.Label(@break));

            var block = Expression.Block(new[] { args, scope, fVar, sVar, varVar }, body.ToArray());

            return block;
        }

        static Expression CompileStatement(IStatement stat, LabelTarget returnTarget, LabelTarget breakTarget, Expression Context)
        {
            if (stat is Ast.Assignment)
                return CompileAssignment(stat as Ast.Assignment, Context);
            else if (stat is Ast.LocalAssignment)
                return CompileLocalAssignment(stat as Ast.LocalAssignment, Context);
            else if (stat is Ast.FunctionCall)
                return CompileFunctionCallStat(stat as Ast.FunctionCall, Context);
            else if (stat is Ast.Block)
                return CompileBlock(stat as Ast.Block, returnTarget, breakTarget, Context);
            else if (stat is Ast.IfStat)
                return CompileIfStat(stat as Ast.IfStat, returnTarget, breakTarget, Context);
            else if (stat is Ast.ReturnStat)
                return CompileReturnStat(stat as Ast.ReturnStat, returnTarget, Context);
            else if (stat is Ast.BreakStat)
                return Expression.Break(breakTarget);
            else if (stat is Ast.WhileStat)
                return CompileWhileStat(stat as Ast.WhileStat, returnTarget, Context);
            else if (stat is Ast.RepeatStat)
                return CompileRepeatStatement(stat as Ast.RepeatStat, returnTarget, Context);
            else if (stat is Ast.GenericFor)
                return CompileGenericFor(stat as Ast.GenericFor, returnTarget, Context);
            else if (stat is Ast.NumericFor)
                return CompileNumericFor(stat as Ast.NumericFor, returnTarget, Context);
            else
                throw new NotImplementedException();
        }
        #endregion
    }
}
