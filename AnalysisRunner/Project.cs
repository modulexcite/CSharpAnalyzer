using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Analysis;
using RoslynUtilities;

namespace AnalysisRunner
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public Enums.ProjectType Type { get; set; }

        public int SLOC { get; set; }

        public bool IsAnalyzable { get; set; }

        public List<AnalysisResult> AnalysisResults { get; set; }
	}
}
