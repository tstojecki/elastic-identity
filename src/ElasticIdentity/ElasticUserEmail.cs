using Nest;

namespace ElasticIdentity
{
	public class ElasticUserEmail : ElasticUserConfirmed
	{
        [Keyword(IncludeInAll = false )]
        public string Address { get; set; }
	}
}