﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynUtilities;

namespace Analysis
{
    public class ConcurrencyUsageWalker : AnalysisWalker
    {
		public ConcurrencyUsageResult Result { get; set; }

        public ConcurrencyUsageWalker(Document sourceFile, SemanticModel semanticModel, ConcurrencyUsageResult result)
            : base(sourceFile, semanticModel)
        {
			Result = result;
		}

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.IsAsync())
            {
                Result.Async++;
				if (node.ReturnsVoid())
				{
					Result.AsyncReturnVoid++;
				}
            }
			base.VisitMethodDeclaration(node);
		}

		public override void VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

			if (symbol != null)
			{
                IsAsyncLibraryConstruct(symbol.OriginalDefinition);

                if (symbol.IsAPMBeginMethod())
                {
                    Logs.TempLog.Info(SourceFile.FilePath + "\n" + node + "\n" + symbol + "\n"+ node.FirstAncestorOrSelf<MethodDeclarationSyntax>() + "******************\n");
                    Result.APM++;
                }
                else if (node.IsEAPMethod())
                {
                    Result.EAP++;
                }
                else if (symbol.IsTAPMethod())
                {
                    // printing TAP methods: Logs.TempLog.Info(SourceFile.FilePath + "\n" + node + "\n" + symbol + "******************\n");
                    Result.TAP++;
                }
                else if (symbol.IsThreadStart())
                {
                    Result.ThreadInit++;
                }
                else if (symbol.IsThreadPoolQueueUserWorkItem())
                {
                    Result.ThreadPoolQueue++;
                }
                else if (symbol.IsBackgroundWorkerMethod())
                {
                    Result.BackgroundWorker++;
                }
                else if (symbol.IsAsyncDelegate())
                {
                    Result.AsyncDelegate++;
                }
                else if (symbol.IsTaskCreationMethod())
                {
                    Result.TaskInit++;
                }
                else if (symbol.IsParallelFor())
                {
                    Result.ParallelFor++;
                }
                else if (symbol.IsParallelForEach())
                {
                    Result.ParallelForEach++;
                }
                else if (symbol.IsParallelInvoke())
                {
                    Result.ParallelInvoke++;
                }
			}

			base.VisitInvocationExpression(node);
		}
    
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol) SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                IsAsyncLibraryConstruct(symbol.OriginalDefinition);
            }

            base.VisitObjectCreationExpression(node);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            string name = "LOCK_Statement";
            var libraryUsage = Result.LibraryUsage;
      
            int temp;
            libraryUsage.TryGetValue(name, out temp);
            libraryUsage[name] = ++temp;

            base.VisitLockStatement(node);
        }

        public void IsAsyncLibraryConstruct(IMethodSymbol symbol)
        {
            if (symbol.ContainingNamespace.ToString().Equals("System.Threading.Tasks") ||
                symbol.ContainingNamespace.ToString().Equals("System.Threading") ||
                (symbol.ContainingNamespace.ToString().Equals("System.Linq") && (symbol.ContainingType.ToString().Contains("ParallelQuery") || symbol.ContainingType.ToString().Contains("ParallelEnumerable"))) ||
                symbol.ContainingNamespace.ToString().Equals("System.Collections.Concurrent"))
            {
                var libraryUsage = Result.LibraryUsage;

                int temp;
                string key = symbol.ToString();
                
                libraryUsage.TryGetValue(key, out temp);
                libraryUsage[key] = ++temp;
            }
        }
    }
}