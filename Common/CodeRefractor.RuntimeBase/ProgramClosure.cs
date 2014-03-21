#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeRefractor.CodeWriter.TypeInfoWriter;
using CodeRefractor.CompilerBackend.Optimizations.Common;
using CodeRefractor.CompilerBackend.Optimizations.EscapeAndLowering;
using CodeRefractor.CompilerBackend.OuputCodeWriter;
using CodeRefractor.CompilerBackend.ProgramWideOptimizations;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.Backend;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.Runtime;

#endregion

namespace CodeRefractor.RuntimeBase
{
    public class ProgramClosure
    {
        public List<Type> UsedTypes;
        public MethodInterpreter EntryInterpreter { get; set; }
        public CrRuntimeLibrary Runtime { get; set; }

        public Dictionary<MethodInterpreterKey, MethodInterpreter> MethodClosure = new Dictionary<MethodInterpreterKey, MethodInterpreter>();
        private readonly Dictionary<Type, int> _typeDictionary;

        VirtualMethodTable _virtualMethodTable;

        public ProgramClosure(MethodInfo entryMethod, ProgramOptimizationsTable table, CrRuntimeLibrary crRuntime)
        {
            Runtime = crRuntime;
            EntryInterpreter = entryMethod.Register();

            ResultingInFunctionOptimizationPass.Runtime = Runtime;
            ResultingGlobalOptimizationPass.Runtime = Runtime;
            BuildMethodClosure();
            MetaLinkerOptimizer.ApplyOptimizations(MethodClosure.Values.ToList());
            if(table.Optimize(this))
                MetaLinkerOptimizer.ApplyOptimizations(MethodClosure.Values.ToList());

            BuildMethodClosure();
            if (!UsedTypes.Contains(typeof(object)))
            {
                UsedTypes.Add(typeof(object));
            }
            TypesClosureLinker.SortTypeClosure(UsedTypes, crRuntime);

            var typeTable = new TypeDescriptionTable(UsedTypes);
            _typeDictionary = typeTable.ExtractInformation();

            BuildVirtualMethodTable(typeTable);
        }

        private void BuildVirtualMethodTable(TypeDescriptionTable typeTable)
        {
            _virtualMethodTable = new VirtualMethodTable(typeTable);
            foreach (var type in _typeDictionary.Keys)
            {
                var methods = type.GetMethods();
                foreach (var method in methods)
                {
                    _virtualMethodTable.RegisterMethod(method);
                }
            }
        }

        private void BuildMethodClosure()
        {
            var entryInterpreter = EntryInterpreter;

            var result = TypesClosureLinker.BuildClosureForEntry(entryInterpreter, this);

            MethodClosure = result.MethodInterpreters;
            UsedTypes = result.UsedTypes.ToList();
        }

        public static void ComputeEscapeAnalysis(List<MethodInterpreter> methodClosures)
        {
            var escapeParameters = new AnalyzeParametersAreEscaping();
            var loweringVars = new InFunctionLoweringVars();
            for (var i = 0; i < 2; i++)
            {
                foreach (var methodInterpreter in methodClosures)
                {
                    escapeParameters.Optimize(methodInterpreter);
                }
                Parallel.ForEach(methodClosures, methodInterpreter =>
                    //foreach (var methodInterpreter in methodClosures)
                    loweringVars.Optimize(methodInterpreter));

            }

            
        }

        public StringBuilder BuildFullSourceCode(CrRuntimeLibrary crRuntime)
        {
            ComputeEscapeAnalysis(MethodClosure.Values.ToList());

            return CppCodeGenerator.GenerateSourceStringBuilder(EntryInterpreter, UsedTypes, MethodClosure.Values.ToList(), _virtualMethodTable, crRuntime);
        }
    }
}