﻿#region Usings

using System.Collections.Generic;
using CodeRefractor.Compiler.Optimizations.Common;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Operators;

#endregion

namespace CodeRefractor.Compiler.Optimizations.ReachabilityDfa
{
    public class ReachabilityLines : ResultingOptimizationPass
    {
        private Dictionary<int, int> _labelTable;
        private HashSet<int> _reached;

        public override void OptimizeOperations(MetaMidRepresentation intermediateCode)
        {
            var operations = intermediateCode.LocalOperations;
            _labelTable = InstructionsUtils.BuildLabelTable(operations);
            _reached = new HashSet<int>();
            Interpret(0, operations);
            if (_reached.Count == operations.Count) return;
            Result = true;
            var toDelete = new List<int>();
            for (var i = 0; i < operations.Count; i++)
            {
                if (!_reached.Contains(i))
                    toDelete.Add(i);
            }
            toDelete.Reverse();
            foreach (var i in toDelete)
            {
                operations.RemoveAt(i);
            }
        }

        private int JumpTo(int labelId)
        {
            return _labelTable[labelId];
        }

        private void Interpret(int cursor, List<LocalOperation> operations)
        {
            if (_reached.Contains(cursor))
                return;
            var canUpdate = true;

            while (canUpdate)
            {
                _reached.Add(cursor);
                var operation = operations[cursor];
                switch (operation.Kind)
                {
                    case LocalOperation.Kinds.BranchOperator:
                        var branchOperator = (BranchOperator) operation.Value;
                        Interpret(JumpTo(branchOperator.JumpTo), operations);
                        break;
                    case LocalOperation.Kinds.AlwaysBranch:
                        var jumpTo = (int) operation.Value;
                        Interpret(JumpTo(jumpTo), operations);
                        return;
                }
                cursor++;
                canUpdate = !_reached.Contains(cursor) && cursor < operations.Count;
            }
        }
    }
}