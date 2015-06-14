using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Analysis
{
	public class ConcurrencyUsageResult : AnalysisResult
	{
		public int Async { get; set; }

		public int AsyncReturnVoid { get; set; }

		public int APM { get; set; }

		public int EAP { get; set; }

		public int TAP { get; set; }

		public int ThreadInit { get; set; }

		public int ThreadPoolQueue { get; set; }

		public int BackgroundWorker { get; set; }

		public int AsyncDelegate { get; set; }

		public int TaskInit { get; set; }

		public int ParallelFor { get; set; }

		public int ParallelInvoke { get; set; }

		public int ParallelForEach { get; set; }

		public ConcurrencyUsageResult()
		{
			Type = AnalysisType.ConcurrencyUsage;
		}
	}
}
