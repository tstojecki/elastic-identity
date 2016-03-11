using Newtonsoft.Json.Converters;

namespace ElasticIdentity
{
	public class ElasticEnumConverter : StringEnumConverter
	{
		public ElasticEnumConverter()
		{
			AllowIntegerValues = true;
			CamelCaseText = true;
		}
	}
}