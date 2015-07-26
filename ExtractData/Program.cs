using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using Analysis;
using AnalysisRunner;

namespace ExtractData
{
	class Program
	{
		static CodeAnalysisResults context = new CodeAnalysisResults();
		static string basicSummaryFile = @"C:\Users\t-seok\Desktop\basicSummary.csv";
		static string concurrencyResultFile = @"C:\Users\t-seok\Desktop\concurrencyResultFile.csv";


		static void Main(string[] args)
		{
			var apps = context.AppResults.Include(a => a.Projects.Select(b => b.AnalysisResults)).ToList();

			ExtractBasicInfo(apps);
			ExtractResult(apps, AnalysisType.ConcurrencyUsage, concurrencyResultFile);
			Console.WriteLine("Total SLOC" + TotalSLOC());
			Console.WriteLine("******FINISHED*******");
			Console.ReadKey();
		}

		private static void ExtractResult(List<App> apps, AnalysisType type, string fileName)
		{
			using (StreamWriter file = new StreamWriter(fileName))
			{
				AnalysisResult temp = AnalysisResultFactory.Generate(type);

				file.WriteLine(App.ToStringColumns() + temp.ToStringColumns());
				foreach (var app in apps)
				{
					string info = app.ToString();
					if (app.Projects.Count() == 0)
					{
						info += temp;
					}
					else
					{
						var list = app.Projects.Where(p => p.AnalysisResults.Any(a => a.Type == type))
							.Select(p => p.AnalysisResults.Single(a => a.Type == type)).ToList();
						info += list.Any() ? list.Aggregate((x, y) => x + y) : temp;
                    }
					file.WriteLine(info);
				}
			}
		}

		static void ExtractBasicInfo(List<App> apps)
		{
			using (StreamWriter file = new StreamWriter(basicSummaryFile))
			{
				file.WriteLine(App.ToStringColumns());
				foreach (var app in apps)
				{
					file.WriteLine(app.ToString());
				}
			}
		}

		static int TotalSLOC()
		{
			return context.AppResults.Select(a => a.Projects.Select(b => b.SLOC).Sum()).Sum();
		}
	}
}
