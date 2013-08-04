﻿#region Usings

using System.Collections.Generic;
using CodeRefractor.Compiler.Optimizations.Common;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.Methods;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Operators;

#endregion

namespace CodeRefractor.Compiler.Optimizations.ConstantFoldingAndPropagation.ComplexAssignments
{
    internal class DeleteVregAssignedAndUsedNextLine : ResultingOptimizationPass
    {
        private readonly HashSet<int> _instructionsToBeDeleted = new HashSet<int>();
        private readonly HashSet<int> _vregToBeDeleted = new HashSet<int>();
        private int _currentId;
        private int _currentRow;
        private MetaMidRepresentation _intermediateCode;
        private LocalVariable _leftVreg;

        public override void OptimizeOperations(MetaMidRepresentation intermediateCode)
        {
            var operations = intermediateCode.LocalOperations;
            _intermediateCode = intermediateCode;
            for (var i = 0; i < operations.Count - 1; i++)
            {
                var srcOperation = operations[i];
                _currentRow = i;
                if (srcOperation.Kind != LocalOperation.Kinds.Assignment &&
                    srcOperation.Kind != LocalOperation.Kinds.LoadArgument)
                    continue;
                var assignment = (Assignment) srcOperation.Value;
                _leftVreg = assignment.Left;
                if (_leftVreg.Kind != VariableKind.Vreg) continue;
                _currentId = _leftVreg.Id;

                ProcessOperation(operations[i + 1], assignment);
            }
            if (_instructionsToBeDeleted.Count == 0)
                return;
            CleanVRegs(intermediateCode);
        }

        private void CleanVRegs(MetaMidRepresentation intermediateCode)
        {
            intermediateCode.DeleteInstructions(_instructionsToBeDeleted);

            _vregToBeDeleted.Clear();
        }

        private void ProcessOperation(LocalOperation destOperation, Assignment assignment)
        {
            switch (destOperation.Kind)
            {
                case LocalOperation.Kinds.Assignment:
                    HandleAssignment(assignment.Right, destOperation.GetAssignment());
                    break;
                case LocalOperation.Kinds.Call:
                    HandleCall(assignment.Right, (MethodData) destOperation.Value);
                    break;
                case LocalOperation.Kinds.Return:
                    HandleReturn(assignment.Right, destOperation);
                    break;
                case LocalOperation.Kinds.BinaryOperator:
                    HandleOperator(assignment.Right, destOperation.GetAssignment());
                    break;
                case LocalOperation.Kinds.BranchOperator:
                    HandleBranchOperator(assignment.Right, (BranchOperator) destOperation.Value);
                    break;
                default:
                    return;
            }
        }

        private static void HandleReturn(IdentifierValue right, LocalOperation assignment)
        {
            assignment.Value = right;
        }

        private void HandleBranchOperator(IdentifierValue right, BranchOperator value)
        {
            if (VarMatchVreg(value.CompareValue))
                value.CompareValue = right;
        }

        private void HandleOperator(IdentifierValue right, Assignment value)
        {
            var binaryOperator = value.Right as BinaryOperator;
            var unaryOperator = value.Right as UnaryOperator;
            if (unaryOperator != null)
            {
                if (VarMatchVreg(unaryOperator.Left))
                    unaryOperator.Left = right;
                return;
            }
            if (binaryOperator == null) return;
            if (VarMatchVreg(binaryOperator.Left))
            {
                binaryOperator.Left = right;
            }
            if (VarMatchVreg(binaryOperator.Right))
            {
                binaryOperator.Right = right;
            }
        }

        private void HandleCall(IdentifierValue right, MethodData methodData)
        {
            for (var index = 0; index < methodData.Parameters.Count; index++)
            {
                var parameter = methodData.Parameters[index];
                if (VarMatchVreg(parameter))
                    methodData.Parameters[index] = right;
            }
        }

        private void HandleAssignment(IdentifierValue right, Assignment assignment)
        {
            if (VarMatchVreg(assignment.Right))
                assignment.Right = right;
        }


        private bool VarMatchVreg(IdentifierValue identifier)
        {
            var localVar = identifier as LocalVariable;
            if (localVar == null)
            {
                return false;
            }
            if (localVar.Kind != VariableKind.Vreg || localVar.Id != _currentId)
                return false;
            DeletVreg();
            return true;
        }

        private void DeletVreg()
        {
            Result = true;
            var operations = _intermediateCode.LocalOperations;
            for (var i = _currentRow + 1; i < operations.Count; i++)
            {
                var operation = operations[i];
                if (operation.IsBranchOperation())
                    break;
                if (operation.OperationUses(_leftVreg))
                    return;
            }
            _vregToBeDeleted.Add(_leftVreg.Id);
            _instructionsToBeDeleted.Add(_currentRow);
        }
    }
}