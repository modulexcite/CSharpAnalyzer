using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analysis
{
	[Flags]
	public enum AnalysisType {
		None = 0,
		ConcurrencyUsage = 1,
		AsyncAwaitAntiPatterns = 2,
	};
}
