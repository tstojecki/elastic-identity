using Nest;

namespace ElasticIdentity
{
	public class ElasticUserEmail : ElasticUserConfirmed
	{
        private string address;

        [Keyword]
        public string Address
        {
            get { return address; }
            set { address = value?.ToLowerInvariant(); }
        }
	}
}