#region Usings

using System.Collections.Generic;
using System.Linq;
using CodeRefractor.CompilerBackend.Optimizations.Inliner;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.Config;
using CodeRefractor.RuntimeBase.FrontEnd;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.Optimizations;

#endregion

namespace CodeRefractor.CompilerBackend.OuputCodeWriter
{
    public static class MetaLinkerOptimizer
    {
        public static void OptimizeMethods()
        {
            LinkerInterpretersTable.Clear();
            
            var methodsToOptimize = LinkerInterpretersTable.Methods;
            ApplyOptimizations(methodsToOptimize.Values.ToList());
        }

        public static void ApplyOptimizations(List<MethodInterpreter> methodsToOptimize)
        {
            bool doOptimize;
            do
            {
                doOptimize = false;
                var toRemove = methodsToOptimize.Where(mth => mth== null).ToArray();
                foreach (var item in toRemove)
                {
                    methodsToOptimize.Remove(item);
                }
                foreach (var methodBase in methodsToOptimize)
                {
                    var interpreter = methodBase;
                    doOptimize = MethodInterpreterCodeWriter.ApplyLocalOptimizations(
                        CommandLineParse.SortedOptimizations[OptimizationKind.InFunction], interpreter);
                }
                foreach (var methodBase in methodsToOptimize)
                {
                    var interpreter = methodBase;
                    doOptimize = MethodInterpreterCodeWriter.ApplyLocalOptimizations(
                        CommandLineParse.SortedOptimizations[OptimizationKind.Global], interpreter);
                }
            } while (doOptimize);

        
        }


    }
}