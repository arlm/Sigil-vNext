﻿using Sigil.Impl;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        private void InjectTailCall()
        {
            for (var i = 0; i < IL.Index; i++)
            {
                var instr = IL[i];
                BufferedILInstruction call = null;

                if (instr.IsInstruction == OpCodes.Ret)
                {
                    int callIx = -1;

                    for (var j = i - 1; j >= 0; j--)
                    {
                        var atJ = IL[j];

                        if (atJ.MarksLabel != null)
                        {
                            break;
                        }

                        if (atJ.IsInstruction.HasValue)
                        {
                            if (ExtensionMethods.IsTailableCall(atJ.IsInstruction.Value))
                            {
                                callIx = j;
                                call = atJ;
                            }

                            break;
                        }
                    }

                    if (callIx == -1) continue;
                    if (call.TakesManagedPointer()) continue;
                    if (call.TakesTypedReference()) continue;

                    InsertInstruction(callIx, OpCodes.Tailcall);
                    i++;
                }
            }
        }

        /// <summary>
        /// Calls the method being constructed by the given emit.  Emits so used must have been constructed with BuildMethod or related methods.
        /// 
        /// Pops its arguments in reverse order (left-most deepest in the stack), and pushes the return value if it is non-void.
        /// 
        /// If the given method is an instance method, the `this` reference should appear before any parameters.
        /// 
        /// Call does not respect overrides, the implementation defined by the given MethodInfo is what will be called at runtime.
        /// 
        /// To call overrides of instance methods, use CallVirtual.
        /// 
        /// When calling VarArgs methods, arglist should be set to the types of the extra parameters to be passed.
        /// </summary>
        public Emit<DelegateType> Call<MethodEmit>(Emit<MethodEmit> emit, Type[] arglist = null)
        {
            if (emit == null)
            {
                throw new ArgumentNullException("emit");
            }

            if (emit.MtdBuilder == null)
            {
                throw new InvalidOperationException("emit must be building a method");
            }

            if (HasFlag(emit.CallingConventions, CallingConventions.VarArgs) && !HasFlag(emit.CallingConventions, CallingConventions.Standard))
            {
                if (arglist == null)
                {
                    throw new InvalidOperationException("When calling a VarArgs method, arglist must be set");
                }
            }

            var expectedParams = ((LinqArray<Type>)emit.ParameterTypes).Select(s => TypeOnStack.Get(s)).ToList();

            if (arglist != null)
            {
                expectedParams.AddRange(((LinqArray<Type>)arglist).Select(t => TypeOnStack.Get(t)));
            }

            // Instance methods expect this to preceed parameters
            if (HasFlag(emit.CallingConventions, CallingConventions.HasThis))
            {
                var declaring = emit.MtdBuilder.DeclaringType;

                if (declaring.IsValueType)
                {
                    declaring = declaring.MakePointerType();
                }

                expectedParams.Insert(0, TypeOnStack.Get(declaring));
            }

            var resultType = emit.ReturnType == TypeOnStack.Get(typeof(void)) ? null : emit.ReturnType;

            var firstParamIsThis =
                HasFlag(emit.CallingConventions, CallingConventions.HasThis) ||
                HasFlag(emit.CallingConventions, CallingConventions.ExplicitThis);

            IEnumerable<StackTransition> transitions;
            if (resultType != null)
            {
                transitions =
                    new[]
                    {
                        new StackTransition(expectedParams.Reverse().AsEnumerable(), new [] { resultType })
                    };
            }
            else
            {
                transitions =
                    new[]
                    {
                        new StackTransition(expectedParams.Reverse().AsEnumerable(), new TypeOnStack[0])
                    };
            }

            UpdateState(OpCodes.Call, emit.MtdBuilder, Wrap(transitions, "Call"), firstParamIsThis: firstParamIsThis, arglist: arglist);

            return this;
        }

        /// <summary>
        /// Calls the given method.  Pops its arguments in reverse order (left-most deepest in the stack), and pushes the return value if it is non-void.
        /// 
        /// If the given method is an instance method, the `this` reference should appear before any parameters.
        /// 
        /// Call does not respect overrides, the implementation defined by the given MethodInfo is what will be called at runtime.
        /// 
        /// To call overrides of instance methods, use CallVirtual.
        /// 
        /// When calling VarArgs methods, arglist should be set to the types of the extra parameters to be passed.
        /// </summary>
        public Emit<DelegateType> Call(MethodInfo method, Type[] arglist = null)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (HasFlag(method.CallingConvention, CallingConventions.VarArgs) && !HasFlag(method.CallingConvention, CallingConventions.Standard))
            {
                if (arglist == null)
                {
                    throw new InvalidOperationException("When calling a VarArgs method, arglist must be set");
                }
            }

            var expectedParams = ((LinqArray<ParameterInfo>)method.GetParameters()).Select(s => TypeOnStack.Get(s.ParameterType)).ToList();

            if (arglist != null)
            {
                expectedParams.AddRange(((LinqArray<Type>)arglist).Select(t => TypeOnStack.Get(t)));
            }

            // Instance methods expect this to preceed parameters
            if (HasFlag(method.CallingConvention, CallingConventions.HasThis))
            {
                var declaring = method.DeclaringType;

                if (declaring.IsValueType)
                {
                    declaring = declaring.MakePointerType();
                }

                expectedParams.Insert(0, TypeOnStack.Get(declaring));
            }

            var resultType = method.ReturnType == typeof(void) ? null : TypeOnStack.Get(method.ReturnType);

            var firstParamIsThis =
                HasFlag(method.CallingConvention, CallingConventions.HasThis) ||
                HasFlag(method.CallingConvention, CallingConventions.ExplicitThis);

            IEnumerable<StackTransition> transitions;
            if (resultType != null)
            {
                transitions =
                    new[]
                    {
                        new StackTransition(expectedParams.Reverse().AsEnumerable(), new [] { resultType })
                    };
            }
            else
            {
                transitions =
                    new[]
                    {
                        new StackTransition(expectedParams.Reverse().AsEnumerable(), new TypeOnStack[0])
                    };
            }

            UpdateState(OpCodes.Call, method, Wrap(transitions, "Call"), firstParamIsThis: firstParamIsThis, arglist: arglist);

            return this;
        }
    }
}
