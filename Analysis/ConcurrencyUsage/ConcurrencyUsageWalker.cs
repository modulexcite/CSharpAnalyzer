using Microsoft.CodeAnalysis;
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
				Logs.AsyncMisuse.Info($"{SourceFile.FilePath}{node.ToLog()}");

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
				if (symbol.IsAPMBeginMethod())
					Result.APM++;
				else if (node.IsEAPMethod())
					Result.EAP++;
				else if (symbol.IsTAPMethod())
				{
					Logs.TempLog.Info(SourceFile.FilePath + "\n" + node +"\n"+ symbol + "******************\n");
					Result.TAP++;
				}
				else if (symbol.IsThreadStart())
					Result.ThreadInit++;
				else if (symbol.IsThreadPoolQueueUserWorkItem())
					Result.ThreadPoolQueue++;
				else if (symbol.IsBackgroundWorkerMethod())
					Result.BackgroundWorker++;
				else if (symbol.IsAsyncDelegate())
					Result.AsyncDelegate++;
				else if (symbol.IsTaskCreationMethod())
					Result.TaskInit++;
				else if (symbol.IsParallelFor())
					Result.ParallelFor++;
				else if (symbol.IsParallelForEach())
					Result.ParallelForEach++;
				else if (symbol.IsParallelInvoke())
					Result.ParallelInvoke++;
			}

			base.VisitInvocationExpression(node);
		}
	}
}