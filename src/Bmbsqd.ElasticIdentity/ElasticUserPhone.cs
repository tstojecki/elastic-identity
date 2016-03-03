﻿using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserPhone : ElasticUserConfirmed
	{
        [String( Index = FieldIndexOption.NotAnalyzed, DocValues = true, IncludeInAll = false )]
        public string Number { get; set; }
	}
}