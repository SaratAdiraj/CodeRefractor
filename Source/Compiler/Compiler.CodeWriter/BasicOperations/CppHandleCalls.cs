﻿#region Usings

using System;
using System.Linq;
using System.Text;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.FrontEnd;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.Methods;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;
using Compiler.CodeWriter.Linker;

#endregion

namespace Compiler.CodeWriter.BasicOperations
{
    internal static class CppHandleCalls
    {
        public static void HandleReturn(LocalOperation operation, StringBuilder bodySb)
        {
            var returnValue = operation.Value as IdentifierValue;

            if (returnValue == null)
                bodySb.Append("return;");
            else
                bodySb.AppendFormat("return {0};", returnValue.Name);
        }


        public static void HandleCall(LocalOperation operation, StringBuilder sbCode, MidRepresentationVariables vars)
        {
            var operationData = (MethodData) operation.Value;
            var sb = new StringBuilder();
            var methodInfo = operationData.Info.GetReversedMethod();

            #region Write method name

            var isVoidMethod = methodInfo.GetReturnType().IsVoid();
            if (isVoidMethod)
            {
                sb.AppendFormat("{0}", methodInfo.ClangMethodSignature());
            }
            else
            {
                if (operationData.Result == null)
                {
                    sb.AppendFormat("{0}", methodInfo.ClangMethodSignature());
                }
                else
                {
                    sb.AppendFormat("{1} = {0}", methodInfo.ClangMethodSignature(),
                        operationData.Result.Name);
                }
            }
            var identifierValues = operationData.Parameters;

            var escapingData = CppFullFileMethodWriter.BuildEscapingBools(methodInfo);
            if (escapingData == null)
            {
                var argumentsCall = String.Join(", ", identifierValues.Select(p =>
                {
                    var computeValue = p.ComputedValue();
                    return computeValue;
                }));

                sb.AppendFormat("({0});", argumentsCall);
                return;
            }

            #endregion

            sb.Append("(");

            #region Parameters

            var pos = 0;
            var isFirst = true;
            var argumentTypes = operationData.Info.GetMethodArgumentTypes();
            foreach (var value in identifierValues)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(", ");
                var localValue = value as LocalVariable;
                var argumentData = argumentTypes[pos];
                var isEscaping = escapingData[pos];
                pos++;
                if (localValue == null)
                {
                    sb.Append(value.ComputedValue());
                    continue;
                }
                if (localValue.Kind == VariableKind.Argument)
                {
                }

                if (localValue.ComputedType().ClrType == typeof (IntPtr))
                {
                    var argumentTypeCast = argumentData.ToCppMangling();
                    sb.AppendFormat("({0}){1}", argumentTypeCast, localValue.Name);
                    continue;
                }

                var localValueData = vars.GetVariableData(localValue);
                switch (localValueData.Escaping)
                {
                    case EscapingMode.Smart:
                        if (!isEscaping && localValue.ComputedType().ClrType.IsClass)
                        {
                            sb.AppendFormat("{0}.get()", localValue.Name);
                        }
                        else
                        {
                            sb.AppendFormat("{0}", localValue.Name);
                        }
                        continue;
                    case EscapingMode.Stack:
                        sb.AppendFormat("&{0}", localValue.Name);
                        continue;

                    case EscapingMode.Pointer:
                        sb.AppendFormat(!isEscaping ? "{0}" : "{0}.get()", localValue.Name);
                        continue;
                }
            }

            sb.Append(");");

            #endregion

            sbCode.Append(sb);
        }

        public static void HandleCallRuntime(LocalOperation operation, StringBuilder sb)
        {
            var operationData = (MethodData) operation.Value;

            var methodInfo = operationData.Info;
            if (methodInfo.IsConstructor)
                return; //don't call constructor for now
            var isVoidMethod = methodInfo.GetReturnType().IsVoid();
            if (isVoidMethod)
            {
                sb.AppendFormat("{0}", methodInfo.ClangMethodSignature());
            }
            else
            {
                sb.AppendFormat("{1} = {0}", methodInfo.ClangMethodSignature(),
                    operationData.Result.Name);
            }
            var identifierValues = operationData.Parameters;
            var argumentsCall = String.Join(", ", identifierValues.Select(p => p.Name));

            sb.AppendFormat("({0});", argumentsCall);
        }
    }
}