using System.ComponentModel;
using Nest;
using Newtonsoft.Json;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserConfirmed
	{
        [Boolean( DocValues = true, NullValue = false )]
        [JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		[DefaultValue( false )]
		public bool IsConfirmed { get; set; }
	}
}