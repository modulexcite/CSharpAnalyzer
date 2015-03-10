using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.IO;
using System;
using Analysis;
using Microsoft.Build.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynUtilities;
using System.Diagnostics;

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
