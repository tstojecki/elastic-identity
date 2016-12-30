using Nest;

namespace ElasticIdentity
{
	public class ElasticUserPhone : ElasticUserConfirmed
	{
        [Keyword]
        public string Number { get; set; }
	}
}