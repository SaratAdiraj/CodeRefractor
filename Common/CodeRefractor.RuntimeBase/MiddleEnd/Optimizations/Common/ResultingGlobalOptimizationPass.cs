#region Usings

using CodeRefractor.Runtime;
using CodeRefractor.RuntimeBase.Optimizations;

#endregion

namespace CodeRefractor.MiddleEnd.Optimizations.Common
{
    public abstract class ResultingGlobalOptimizationPass : ResultingOptimizationPass
    {
        public static CrRuntimeLibrary Runtime { get; set; }

        public ResultingGlobalOptimizationPass()
            : base(OptimizationKind.Global)
        {
        }
    }
}