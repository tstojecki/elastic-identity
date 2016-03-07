using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserEmail : ElasticUserConfirmed
	{
        [String( Index = FieldIndexOption.NotAnalyzed, DocValues = true, IncludeInAll = false )]
        public string Address { get; set; }
	}
}