using System.Reflection;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd;

namespace CodeRefractor.RuntimeBase.FrontEnd
{
    public static class GlobalMethodPoolUtils
    {
        public static void Register(MethodInterpreter interpreter)
        {
            var method = interpreter.Method;
            var methodDefinitionKey = GenerateKey(method);
            var globalMethodPool = GlobalMethodPool.Instance;
            globalMethodPool.Interpreters[methodDefinitionKey] = interpreter;
            globalMethodPool.MethodInfos[methodDefinitionKey] = method;
        }

        public static MethodInterpreter Register(this MethodBase method)
        {
            var interpreter = GetRegisteredInterpreter(method);
            if (interpreter != null)
                return interpreter;
            interpreter = new MethodInterpreter(method);
            Register(interpreter);
            return interpreter;
        }

        public static string GenerateKey(this MethodBase method)
        {
            return method.WriteHeaderMethod(false);
        }

        public static MethodInterpreter GetRegisteredInterpreter(this MethodBase method)
        {
            var methodDefinitionKey = GenerateKey(method);
            var interpreters= GlobalMethodPool.Instance.Interpreters;
            MethodInterpreter result;
            if (!interpreters.TryGetValue(methodDefinitionKey, out result))
                return null;
            return result;
        }

        public static MethodBase GetReversedMethod(this MethodBase methodInfo)
        {
            var reverseType = methodInfo.DeclaringType.GetMappedType();
            if (reverseType == methodInfo.DeclaringType) 
                return methodInfo;
            var originalParameters = methodInfo.GetParameters();
            var memberInfos = reverseType.GetMember(methodInfo.Name);

            foreach (var memberInfo in memberInfos)
            {
                var methodBase = memberInfo as MethodBase;
                if (methodBase == null)
                    continue;
                var parameters = methodBase.GetParameters();
                if (parameters.Length != originalParameters.Length)
                    continue;
                bool found = true;
                for (var index = 0; index < parameters.Length; index++)
                {
                    var parameter = parameters[index];
                    var originalParameter = originalParameters[index];
                    if (parameter.ParameterType == originalParameter.ParameterType) continue;
                    found = false;
                    break;
                }
                if (found)
                {
                    return methodBase;
                }
            }
            return methodInfo;
        }
    }
}