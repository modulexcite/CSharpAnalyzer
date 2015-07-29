using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using RoslynUtilities;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Analysis
{
    public class AsyncAwaitAntiPatternsWalker : AnalysisWalker
	{
		public AsyncAwaitAntiPatternsResult Result { get; set; }

        private static string[] BlockingMethodCalls = { "WaitAll", "WaitAny", "Wait", "Sleep" };

        public AsyncAwaitAntiPatternsWalker(Document sourceFile, SemanticModel semanticModel, AsyncAwaitAntiPatternsResult result)
			: base(sourceFile, semanticModel)
        {
			Result = result;
		}

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
			if (node.IsAsync())
			{
                Logs.AsyncMethods.Info(SourceFile.FilePath + "\n" + node + "\n" + "******************\n");

                DetectBlockingAsyncCallers(node);

                if (IsUnnecessaryAsyncAwait(node))
                {
                    Logs.TempLog.Info("Unnecessary async/await" + "\n" + SourceFile.FilePath + "\n" + node + "\n" + "******************\n");
                }

                string replacement;
                if (IsThereLongRunning(node, out replacement))
                {
                    Logs.TempLog2.Info("Longrunning replacement: " + replacement + "\n" + SourceFile.FilePath + "\n" + node + "\n" + "******************\n");
                }
            }

			base.VisitMethodDeclaration(node);
		}

		public void DetectBlockingAsyncCallers(MethodDeclarationSyntax node)
		{
			var symbol = SemanticModel.GetDeclaredSymbol(node);
			if (symbol != null)
			{
				foreach (var refs in SymbolFinder.FindReferencesAsync(symbol, SourceFile.Project.Solution).Result)
				{
					foreach (var loc in refs.Locations)
					{
						var textSpan = loc.Location.SourceSpan;
						var callerNode = loc.Document.GetSyntaxRootAsync().Result.DescendantNodes(textSpan).FirstOrDefault(n => textSpan.Contains(n.Span));
						var callerText = loc.Document.GetTextAsync().Result.Lines.ElementAt(loc.Location.GetLineSpan().StartLinePosition.Line).ToString();
						if (callerText.Contains(".Wait()")) // caller.Contains(".Result")
						{
							Logs.TempLog5.Info("Blocking Caller Name: " + callerText.Trim() + " from this file: " + loc.Document.FilePath);
							var temp = callerNode != null ? callerNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() : null;
							if (temp != null)
							{
								Logs.TempLog5.Info("Blocking Caller Method Node: " + temp.ToLog());
								if (temp.IsAsync())
								{
									Logs.TempLog5.Info("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
								}
							}

							Logs.TempLog5.Info("Async method Callee from this file " + SourceFile.FilePath + node.ToLog()+ Logs.Break);
						}
					}
				}
			}
		}

		private static bool IsFireForget(MethodDeclarationSyntax node)
		{
			return node.ReturnType.ToString().Equals("void") && !node.HasEventArgsParameter();
		}

		private static bool IsUnnecessaryAsyncAwait(MethodDeclarationSyntax node)
		{
            if(node.AttributeLists.Any(a => a.ToFullString().Contains("TestMethod")))
            {
                return false;
            }

			int numAwaits = Regex.Matches(node.Body.ToString(), "await").Count;
			int numReturnAwaits = Regex.Matches(node.Body.ToString(), "return await").Count;

			if (!node.ReturnType.ToString().Equals("void") &&
				!node.DescendantNodes().OfType<StatementSyntax>().Where(a => a.ToString().Contains("await")).Any(a => a.Ancestors().OfType<TryStatementSyntax>().Any()))
			{
				if (numAwaits == numReturnAwaits)
					return true;
				else if (numAwaits == 1 && node.Body.Statements.Count > 0 && node.Body.Statements.Last().ToString().StartsWith("await"))
					return true;
			}
			return false;
		}

		private bool IsThereLongRunning(MethodDeclarationSyntax node, out string replacement)
		{
            replacement = string.Empty;
            foreach (var blocking in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a => BlockingMethodCalls.Any(b => b.Equals(a.Name.ToString()))))
            {
                replacement = "Blocking Method Calls";
                return true;
            }

			foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				var methodCallSymbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(methodCall).Symbol;
				if (methodCallSymbol != null)
				{
					replacement = DetectSynchronousUsages((IMethodSymbol)methodCallSymbol.OriginalDefinition, SemanticModel);

					if (!string.IsNullOrEmpty(replacement))
					{
                        if (!methodCallSymbol.Name.ToString().Equals("Invoke"))
                        {
                            return true;
                        }
					}
				}
			}

			return false;
		}

		private bool IsUnnecessarilyCaptureContext(MethodDeclarationSyntax node, int n)
		{
			if (CheckUIElementAccess(node))
				return false;
			else
			{
				bool result = true;
				{
					var newMethods = new List<MethodDeclarationSyntax>();
					try
					{
						var semanticModel = SourceFile.Project.Solution.GetDocument(node.SyntaxTree).GetSemanticModelAsync().Result;
						if (semanticModel != null)
						{
							foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
							{
								var methodCallSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(methodCall).Symbol;

								if (methodCallSymbol != null)
								{
									var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
									if (methodDeclarationNode != null && n < 10)
										newMethods.Add(methodDeclarationNode);
								}
							}
						}
						foreach (var newMethod in newMethods)
							result = result && IsUnnecessarilyCaptureContext(newMethod, n + 1);
					}
					catch (Exception ex)
					{

					}
				}
				return result;
			}
		}

        #region Utilities

        public static string DetectSynchronousUsages(IMethodSymbol methodCallSymbol, SemanticModel semanticModel)
        {
            var list = semanticModel.LookupSymbols(0, container: methodCallSymbol.ContainingType,
                                includeReducedExtensionMethods: true);

            var name = methodCallSymbol.Name;

            if (name.Equals("Sleep"))
            {
                return "Task.Delay";
            }

            foreach (var tmp in list)
            {
                if (tmp.Name.Equals(name + "Async"))
                {
                    if (!name.Equals("Invoke"))
                        return tmp.Name;
                }
            }

            return string.Empty;
        }

        private bool CheckUIElementAccess(MethodDeclarationSyntax node)
		{
			foreach (var identifier in node.Body.DescendantNodes().OfType<IdentifierNameSyntax>())
			{
				var symbol = SemanticModel.GetSymbolInfo(identifier).Symbol;

				if (symbol != null)
				{
					if (symbol.ToString().StartsWith("System.Windows.") || symbol.ToString().StartsWith("Microsoft.Phone."))
						return true;
				}


			}
			return false;
		}

        #endregion Utilities

        //private void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n, string topAncestor)
        //{
        //	var hashcode = node.Identifier.ToString() + node.ParameterList.ToString();

        //	bool asyncFlag = false;
        //	if (node.HasAsyncModifier())
        //		asyncFlag = true;

        //	if (!AnalyzedMethods.Contains(hashcode))
        //	{
        //		AnalyzedMethods.Add(hashcode);

        //		var newMethods = new List<MethodDeclarationSyntax>();
        //		try
        //		{
        //			var semanticModel = Document.Project.Solution.GetDocument(node.SyntaxTree).GetSemanticModelAsync().Result;

        //			foreach (var blocking in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a => BlockingMethodCalls.Any(b => b.Equals(a.Name.ToString()))))
        //			{
        //				Logs.TempLog2.Info("BLOCKING {0} {1} {2}\r\n{3} \r\n{4}\r\n{5}\r\n--------------------------", asyncFlag, n, blocking, Document.FilePath, topAncestor, node);
        //			}

        //			foreach (var blocking in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a => a.Name.ToString().Equals("Result")))
        //			{
        //				var s = semanticModel.GetSymbolInfo(blocking).Symbol;
        //				if (s != null && s.ToString().Contains("System.Threading.Tasks"))
        //					Logs.TempLog2.Info("BLOCKING {0} {1} {2}\r\n{3} \r\n{4}\r\n{5}\r\n--------------------------", asyncFlag, n, blocking, Document.FilePath, topAncestor, node);
        //			}

        //			foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        //			{
        //				var methodCallSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(methodCall).Symbol;

        //				if (methodCallSymbol != null)
        //				{
        //					var replacement = ((IMethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

        //					if (replacement != "None")
        //					{
        //						if (!methodCallSymbol.Name.ToString().Equals("Invoke"))
        //							Logs.TempLog2.Info("LONGRUNNING {0} {1} {2} {3}\r\n{4} {5}\r\n{6}\r\n--------------------------", asyncFlag, n, methodCallSymbol, Document.FilePath, replacement, topAncestor, node);
        //						Logs.TempLog3.Info("{0} {1}", methodCallSymbol.ContainingType, methodCallSymbol, replacement);
        //					}

        //					var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
        //					if (methodDeclarationNode != null)
        //						newMethods.Add(methodDeclarationNode);
        //				}
        //			}

        //			foreach (var newMethod in newMethods)
        //				ProcessMethodCallsInMethod(newMethod, n + 1, topAncestor);
        //		}
        //		catch (Exception ex)
        //		{
        //			Logs.Console.Warn("Caught exception while processing method call node: {0} @ {1}", node, ex.Message);

        //			if (!(
        //				  ex is FormatException ||
        //				  ex is ArgumentException ||
        //				  ex is PathTooLongException))
        //				throw;
        //		}
        //	}
        //}
    }
}
