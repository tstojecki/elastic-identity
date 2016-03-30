Elastic Identity - The ASP.NET Identity Provider for ElasticSearch
==================================================================

Why use elastic-identity
========================

Elastic-Identity wires up the storage and repository of ElasticSearch with ASP.NET Identity


Revision History
==========

- 2.0.0-rc1 
  
  Remarks:
  - Bump projects to .NET Framework 4.6.
  - Upgrades:
     - nUnit 2.6.4 and ASP.NET Identity 2.2.1.
     - Elasticsearch.Net/NEST 2.0.4.
  - Optimistic concurrency control support using document _version.
  - Add quorum consistency on all operations sans read/get.
  - Align version with Elasticsearch.

  - Breaking changes:
	 - Renamed namespace from Bmbsqd.ElasticIdentity to ElasticIdentity, renamed assemblies and project names
     - The JSON document representing the user no longer contains the 'id' field in favor of the document's meta value '_id'. The ElasticUser.Id property is still present on the class and will contain the '_id' value. This same principle is used for ElasticUser.Version (via _version).
	 - Breaking change in constructor; no more type parameter. Rely on the class type instead.

- 1.0.0-rc2
  - Fixed version problem with NEST dependency  
- 1.0.0-rc1
  - Added support for additional services: 
     - IUserTwoFactorStore
     - IUserEmailStore
     - IUserPhoneNumberStore
  - Upgrade to ASP.NET Identity 2.x
  - Upgrade to support Nest 1.x
  - Breaking change in constructor, no more seed parameter, users should override SeedAsync() instead

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
Elastic identity will check if the index exists and create it if it isn't. 
You can also specify forceRecreate to true in the ctor to delete the index if it exists.

```csharp
new ElasticUserStore<ElasticUser>(elasticClient, "users-index", true);
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

More samples and documentation
------------------------------
TBD...

Contributing
------------

Yes please

Thanks to [Ry-K](https://github.com/Ry-K) for the 2.0.0 upgrade
Thanks to [tstojecki](https://github.com/tstojecki) for the NEST-RC1 upgrade
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

