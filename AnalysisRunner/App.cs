using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Analysis;
using RoslynUtilities;

namespace AnalysisRunner
{
    public class App
    {
        [Key]
        public int AppId { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public List<Project> Projects { get; set; }

		public AnalysisType CompletedAnalyses { get; set; }

		public override string ToString()
		{
			return $"{Name}, " +
				$"{Projects.Count()}, " +
				$"{Projects.Where(b => b.IsAnalyzable == false).Count()}, " +
				$"{Projects.Where(b => b.Type == Enums.ProjectType.NET4).Count()}, " +
				$"{Projects.Where(b => b.Type == Enums.ProjectType.NET45).Count()}, " +
				$"{Projects.Where(b => b.Type == Enums.ProjectType.NETOther).Count()}, " +
				$"{Projects.Where(b => b.Type == Enums.ProjectType.WP7).Count()}, " +
				$"{Projects.Where(b => b.Type == Enums.ProjectType.WP8).Count()}, " +
				$"{Projects.Where(b => b.Type == Enums.ProjectType.ASPNET).Count()}, " +
				$"{Projects.Where(b => b.Type == Enums.ProjectType.API).Count()}, " +
				$"{Projects.Select(b => b.SLOC).Sum()}, ";
		}

		public static string ToStringColumns()
		{
			return "AppName, Total, Unanalyzed, NET4, NET45, NETOther, WP7, WP8, ASPNET, API, SLOC,";
		}
    }
}