using Nest;

namespace ElasticIdentity
{
	public class ElasticUserPhone : ElasticUserConfirmed
	{
        [Keyword(DocValues = true, IncludeInAll = false )]
        public string Number { get; set; }
	}
}