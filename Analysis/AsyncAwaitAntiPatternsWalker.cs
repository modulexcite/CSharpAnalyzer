using System;
using Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynUtilities;

namespace Analysis
{
    public class AsyncAwaitAntiPatternsWalker : AnalysisWalker
	{
		public AsyncAwaitAntiPatternsResult Result { get; set; }

		public AsyncAwaitAntiPatternsWalker(Document sourceFile, SemanticModel semanticModel, AsyncAwaitAntiPatternsResult result)
			: base(sourceFile, semanticModel)
        {
			Result = result;
		}

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
        }
    }
}
