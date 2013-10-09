﻿using System.Collections.Generic;
using CodeRefractor.CompilerBackend.Optimizations.Common;
using CodeRefractor.CompilerBackend.Optimizations.Purity;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;

namespace CodeRefractor.CompilerBackend.Optimizations.Licm
{
    class LoopInvariantCodeMotion : ResultingGlobalOptimizationPass
    {
        public override bool CheckPreconditions(MetaMidRepresentation midRepresentation)
        {
            var loopStarts = LoopDetection.FindLoops(midRepresentation);
            return loopStarts.Count != 0;
        }
        public override void OptimizeOperations(MetaMidRepresentation intermediateCode)
        {
            List<int> loopStarts;
            bool found;
            do
            {
                loopStarts = LoopDetection.FindLoops(intermediateCode);
                found = false;
                foreach (var loopStart in loopStarts)
                {
                    var loopEnd = LoopDetection.GetEndLoop(intermediateCode.LocalOperations, loopStart);
                    var allDefinedVariables = GetAllDefinedVariables(intermediateCode, loopStart, loopEnd);
                    var allInvariantInstructions = GetAllInvariantInstructions(intermediateCode, loopStart, loopEnd,
                                                                               allDefinedVariables);
                    if (allInvariantInstructions.Count == 0)
                        continue;
                    PerformMoveInstructions(intermediateCode, loopStart, allInvariantInstructions);
                    Result = true;
                    found = true;
                    break;
                }
            }
            while(loopStarts.Count != 0 && found);
        }

        private static void PerformMoveInstructions(MetaMidRepresentation intermediateCode, int loopStart, List<int> allInvariantInstructions)
        {
            var localOps = intermediateCode.LocalOperations;

            var licmBlock = new List<LocalOperation>();
            for (var index = allInvariantInstructions.Count - 1; index >= 0; index--)
            {
                var invariantInstruction = allInvariantInstructions[index];
                licmBlock.Add(localOps[invariantInstruction]);
                localOps.RemoveAt(invariantInstruction);
            }
            for (int index = 0; index < licmBlock.Count; index++)
            {
                var operation = licmBlock[index];
                localOps.Insert(loopStart + index, operation);
            }
        }

        private static List<int> GetAllInvariantInstructions(MetaMidRepresentation intermediateCode, int loopStart, int loopEnd, HashSet<LocalVariable> getAllDefinedVariables)
        {
            var localOps = intermediateCode.LocalOperations;


            var result=new List<int>();
            for (var index = loopStart; index <= loopEnd; index++)
            {
                var op = localOps[index];
                switch (op.Kind)
                {
                    default:continue;
                    case OperationKind.UnaryOperator:
                    case OperationKind.Call:
                    case OperationKind.BinaryOperator:
                    case OperationKind.Assignment:
                        break;
                }
                if(op.Kind==OperationKind.Call)
                {
                    var methodData =EvaluatePureFunctionWithConstantCall.ComputeAndEvaluatePurityOfCall(op);
                    if(!methodData.IsPure)
                        continue;
                }
                var usages = op.GetUsages();
                var isInvariant = true;
                foreach (var usage in usages)
                {
                    if (!getAllDefinedVariables.Contains(usage)) continue;
                    isInvariant = false;
                    break;
                }
                if(!isInvariant)
                    continue;
                result.Add(index);
            }

            return result;

        }

        private HashSet<LocalVariable> GetAllDefinedVariables(MetaMidRepresentation intermediateCode, int loopStart, int loopEnd)
        {
            var localOps = intermediateCode.LocalOperations;
            var result = new HashSet<LocalVariable>();
            for (var index = loopStart; index <= loopEnd; index++)
            {
                var op = localOps[index];
                var definition = op.GetUseDefinition();
                if(definition==null)
                    continue;
                result.Add(definition);
            }

            return result;
        }
    }
}