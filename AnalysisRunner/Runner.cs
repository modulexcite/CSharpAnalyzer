using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using Analysis;
using Microsoft.Build.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynUtilities;

namespace AnalysisRunner
{

	public class CodeAnalysisResults : DbContext
	{
		public DbSet<App> AppResults { get; set; }
	}

	class Runner
    {
        private static string CodeCorpusPath = ConfigurationManager.AppSettings["CodeCorpus"];
        private static string SubsetApps = ConfigurationManager.AppSettings["SubsetApps"];

        public static AnalysisType[] AnalysisTypes = { AnalysisType.AsyncAwaitAntiPatterns };

        private static void Main(string[] args)
        {
            StartAnalysis();
            Console.WriteLine("**************FINISHED***************");
            Console.ReadKey();
        }

        private static void StartAnalysis()
        {
            IEnumerable<string> appsToAnalyze;
            if (bool.Parse(ConfigurationManager.AppSettings["OnlyAnalyzeSubsetApps"]))
            {
                appsToAnalyze = File.ReadAllLines(SubsetApps).Select(appName => CodeCorpusPath + appName);
            }
            else
            {
                appsToAnalyze = Directory.GetDirectories(CodeCorpusPath);
            }

            //appsToAnalyze = appsToAnalyze.Take(2);

            using (var context = new CodeAnalysisResults())
            {
				foreach (var appPath in appsToAnalyze)
				{
					string appName = appPath.Split('\\').Last();

					var app = context.AppResults.Include(a => a.Projects)
						.SingleOrDefault(a => a.Name == appName);
					
					if (app == null)
					{
						app = new App()
						{
							Name = appName,
							Path = appPath,
							Projects = new List<Project>(),
							CompletedAnalyses = AnalysisType.None
						};
						context.AppResults.Add(app);
					}

					var unCompleted = AnalysisTypes.Where(a => (app.CompletedAnalyses & a) != a).ToList();

					if (unCompleted.Any())
					{
						Logs.Console.Info("Analyzing {0}", appPath);
						AnalyzeApp(context, app, unCompleted);
					}
					else
					{
						Logs.Console.Info("Already Analyzed {0}", appPath);
					}

					context.SaveChanges();
                }

            }
        }

		private static void AnalyzeApp(CodeAnalysisResults context, App app, List<AnalysisType> analyses)
		{
			var projectPaths = from f in Directory.GetFiles(app.Path, "*.csproj", SearchOption.AllDirectories)
							   let directoryName = Path.GetDirectoryName(f)
							   where !directoryName.Contains(@"\tags") &&
									 !directoryName.Contains(@"\branches")
							   select f;

			foreach (var projectPath in projectPaths)
			{
				string[] separator = new string[] { ".csproj" };
				string name = projectPath.Split('\\').Last().Split(separator, StringSplitOptions.None).First();

				Project project = app.Projects.SingleOrDefault(a=> a.Path == projectPath);

				if (project == null)
				{
					project = new Project()
					{
						Name = projectPath.Split('\\').Last().Split(separator, StringSplitOptions.None).First(),
						Path = projectPath,
						AnalysisResults = new List<AnalysisResult>()
					};
					app.Projects.Add(project);
				}
				else
				{
					context.Entry(project).Collection(a => a.AnalysisResults).Load();
				}

				AnalyzeProject(project, analyses);
			}

			foreach (var analysisType in analyses)
			{
				app.CompletedAnalyses |= analysisType;
			}
		}

		private static void AnalyzeProject(Project project, List<AnalysisType> analyses)
		{
			project.SLOC = 0;
			using (var workspace = MSBuildWorkspace.Create())
			{
				try
				{
					var compiledProject = workspace.OpenProjectAsync(project.Path).Result;
					if (!compiledProject.HasDocuments && !compiledProject.IsCSharpProject())
					{
						return;
					}

					project.Type = compiledProject.GetProjectType();
					var sourceFiles = compiledProject.Documents;

					foreach (var analysisType in analyses)
					{
						project.AnalysisResults.Add(AnalysisResultFactory.Generate(analysisType));
					}

					foreach (var sourceFile in sourceFiles)
					{
						AnalyzeSourceFile(project, sourceFile, analyses);
					}

					project.IsAnalyzable = true;
				}
				catch (Exception ex)
				{
					project.IsAnalyzable = false;
					if (ex is InvalidProjectFileException ||
						ex is FormatException ||
						ex is ArgumentException ||
						ex is PathTooLongException ||
						ex is AggregateException)
					{
						Logs.ErrorLog.Info("Project not analyzed: {0}: Reason: {1}", project.Path, ex.Message);
					}
					else
						throw;
				}
			}
		}

		private static void AnalyzeSourceFile(Project project, Document sourceFile, List<AnalysisType> analyses)
		{
			var root = (SyntaxNode)sourceFile.GetSyntaxRootAsync().Result;
			var semanticModel = (SemanticModel)sourceFile.GetSemanticModelAsync().Result;
			var sloc = sourceFile.GetTextAsync().Result.Lines.Count;

			try
			{
				foreach (var analysisType in analyses)
				{
					
					AnalysisResult result = project.AnalysisResults.Single(a => a.Type == analysisType);
					AnalysisWalker walker = AnalysisWalkerFactory.Generate(sourceFile, semanticModel, result, analysisType);
					walker.Visit(root);
				}

				project.SLOC += sloc;
			}
			catch (InvalidProjectFileException ex)
			{
				Logs.ErrorLog.Info("SourceFile is not analyzed: {0}: Reason: {1}", sourceFile.FilePath, ex.Message);
			}
		}

	}
}