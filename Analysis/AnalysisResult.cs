using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Analysis
{
	// Analysis Result per Project
	public abstract class AnalysisResult
	{
		[Key]
		public int AnalysisId { get; set; }

		public AnalysisType Type { get; set; }

		public override string ToString()
		{
			Type type = this.GetType();

			string result = "";
			foreach (var property in FilterProperties(type))
			{
				result += property.GetValue(this, null) + ", ";
			}

			return result;
		}

		public virtual string ToStringColumns()
		{
			Type type = this.GetType();

			string result = "";
			foreach (var property in FilterProperties(type))
			{
				result += property.Name + ", ";
			}

			return result;
		}

		public static AnalysisResult operator +(AnalysisResult c1, AnalysisResult c2)
		{
			Type type = c1.GetType();
			AnalysisResult instance = (AnalysisResult)Activator.CreateInstance(type);

			foreach (var property in FilterProperties(type))
			{
				int value = (int)property.GetValue(c1, null) + (int)property.GetValue(c2, null);
				property.SetValue(instance, value);
			}
			return instance;
		}

        /// <summary>
        /// Getting the list of all properties of the given AnalysisResult.
        /// </summary>
		public static List<PropertyInfo> FilterProperties(Type type)
		{
			return type.GetProperties().Where(a => a.Name != "AnalysisId" && a.Name != "Type").ToList();
        }
	}

	public static class AnalysisResultFactory
	{
		public static AnalysisResult Generate(AnalysisType type)
		{
			switch (type)
			{
				case AnalysisType.ConcurrencyUsage:
					return new ConcurrencyUsageResult();
				case AnalysisType.AsyncAwaitAntiPatterns:
					return new AsyncAwaitAntiPatternsResult();
				default:
					throw new NotImplementedException(type.ToString());
			}
		}
	}

}
