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
	 - Bracking change in renaming namespace from Bmbsqd.ElasticIdentity to ElasticIdentity
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
Get it from [nuget.org](https://www.nuget.org/packages/Bmbsqd.ElasticIdentity)

Simple
------

```csharp
new ElasticUserStore<ElasticUser>( new Uri( "http://localhost:9200/" ) );
```

With AutoFac
------------

```csharp
builder.Register( c => new ElasticUserStore<ElasticUser>( Settings.Default.UserServer ) )
	.AsSelf()
	.AsImplementedInterfaces()
	.SingleInstance();
```

You should probably consume IUserStore<ElasticUser> in your code


Let's seed the user store if the index is created
-------------------------------------------------

```csharp

public class MyElasticUserStore<TUser> : ElasticUserStore<TUser>
{
  public MyElasticUserStore( Uri connectionString ) : base( connectionString )
  {
  }
  
  static readonly string[] _roles =
  {
    "Admin", 
    "User", 
    ...
  };

  const string _seedUser = "elonmusk";
  const string _seedPassword = "tesla";

  protected override async Task SeedAsync()
  {
    var user = new ElasticUser {
      UserName = _seedUser
    };
    user.Roles.UnionWith( _roles );

    var userManager = new UserManager<ElasticUser>( this );
    await userManager.CreateAsync( user, _seedPassword );
  }
}


builder.Register( c => new MyElasticUserStore<ElasticUser>( Settings.Default.UserServer ) )
	.AsSelf()
	.AsImplementedInterfaces()
	.SingleInstance();
```


Extend the user
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


new ElasticUserStore<MyUser>( new Uri( "http://localhost:9200/" ) );
```

More samples and documentation
------------------------------

The code is pretty tiny, so go ahead and explore the source code to find out what's possible.
Also, check out the options of the ElasticUserStore constructor

```csharp
ElasticUserStore( 
	Uri connectionString,							// where's your elasticsearch. Something like http://localhost:9200/ or http://users.tesla-co.internal/
	string indexName = "users",						// what index we're storing the users under. Defaults to "users"
	bool forceRecreate = false						// if index exists, drop it before creating it again.
	 )

protected override async Task SeedAsync()
{
  // Put your seeding logic here, stuff that's 
  // executed when the index is created 
}

```

Contributing
------------

Yes please

Thanks to [tstojecki](https://github.com/tstojecki) for the NEST-RC1 upgrade

History
-------
The first version of the library was developed by [bmbsqd] (http://github.com/bmbsqd). 

As of ver 2.0.0, the ownership was transfered to [tstojecki] (github.com/tstojecki). Starting with version 2.0.0, a new package has been built and published up on nuget. The namespace and the assembly names were also changed to ElasticIdentity from Bmbsqd.ElasticIdentity.
If you want to previous version of the library, you the older version of the package and the source code remain available under their original locations.

Copyright and license
---------------------

elastic-identity is licenced under the MIT license. Refer to LICENSE for more information.

