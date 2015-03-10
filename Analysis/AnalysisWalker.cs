using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis
{
    public abstract class AnalysisWalker : CSharpSyntaxWalker
    {
        public SemanticModel SemanticModel { get; set; }

        public Document SourceFile { get; set; }

        public AnalysisWalker(Document sourceFile, SemanticModel semanticModel)
        {
            SourceFile = sourceFile;
            SemanticModel = semanticModel;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if ((node.BaseList != null) && (node.BaseList.ToString().Contains("ClientBase") || node.BaseList.ToString().Contains("ChannelBase")))
            {
                // IGNORE WCF SERVICES WHICH ARE GENERATED AUTOMATICALLY
            }
            else
                base.VisitClassDeclaration(node);
        }
    }

	public static class AnalysisWalkerFactory
	{
		public static AnalysisWalker Generate(Document sourceFile, SemanticModel semanticModel, AnalysisResult result, AnalysisType type)
		{
			switch (type)
			{
				case AnalysisType.ConcurrencyUsage:
					return new  ConcurrencyUsageWalker(sourceFile, semanticModel, (ConcurrencyUsageResult)result);
				case AnalysisType.AsyncAwaitAntiPatterns:
					return new AsyncAwaitAntiPatternsWalker(sourceFile, semanticModel, (AsyncAwaitAntiPatternsResult)result);
				default:
					throw new NotImplementedException(type.ToString());
			}
		}
	}

}
