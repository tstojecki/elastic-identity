Elastic Identity - The ASP.NET Identity Provider for ElasticSearch
==================================================================

Why use elastic-identity
========================

Elastic-Identity wires up the storage and repository of ElasticSearch with ASP.NET Identity

Versions
==========

- 5.2.0
	- ElasticSearch.Net/NEST 5.2.0

- 5.0.0
	- ElasticSearch.Net/NEST 5.0.0

- 2.0.1-rc1 & 2.0.1-rc1 & 2.0.3-rc1
	- Elasticsearch.Net/NEST 2.3.1

- 2.0.0-rc1 
	- Elasticsearch.Net/NEST 2.0.4.

- 1.0.0-rc1 & 1.0.0-rc2
	- Elasticsearch.Net/NEST 1.x

How to use
==========

Install
-------
Get it from [nuget.org](https://www.nuget.org/packages/ElasticIdentity/)

Simple
------

```csharp
new ElasticUserStore<ElasticUser>(new Uri("http://localhost:9200/"));
```

or if you have configured an instance of ElasticClient somewehere else

```csharp
new ElasticUserStore<ElasticUser>(elasticClient);
```

Ensure index
-------------------------------------------------
Call EnsureIndex() to check if the index exists and create it if it doesn't. 
You can force the index to be recreated through the forceCreate parameter.

```csharp
var store = new ElasticUserStore<ElasticUser>(elasticClient);
store.EnsureIndex();
```

Extend user
---------------

```csharp
public enum Transportation {
	Roadster,
	ModelS,
	Other
}


public class MyUser : ElasticUser
{
	public string Twitter { get; set; }
	public Transportation Car { get; set; }
}


new ElasticUserStore<MyUser>(new Uri("http://localhost:9200/"));
```

Contributing
------------

Yes please

Thanks to [chandy21](https://github.com/chandy21) for the help with 5.0.0 upgrade
Thanks to [Ry-K](https://github.com/Ry-K) for the help with 2.0.0 upgrade
Thanks to [tstojecki](https://github.com/tstojecki) for the help with NEST-RC1 upgrade
Thanks to [bmbsqd](https://github.com/bmbsqd) for version 1.0.0-rc1

History
-------
The first version of the library was developed by [bmbsqd] (http://github.com/bmbsqd). 

As of version 2.0.0, the ownership was transfered to [tstojecki]. 
Starting with version 2.0.0, a new package has been built and published up on nuget. The namespace and the assembly names were also changed to ElasticIdentity from Bmbsqd.ElasticIdentity.
The older version continues to be available on nuget and the source code on github.com/bmbsqd.

Copyright and license
---------------------

elastic-identity is licenced under the MIT license. Refer to LICENSE for more information.

