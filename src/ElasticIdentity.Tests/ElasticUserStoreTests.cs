#region MIT License

// /*
// 	The MIT License (MIT)
// 
// 	Copyright (c) 2013 Bombsquad Inc
// 
// 	Permission is hereby granted, free of charge, to any person obtaining a copy of
// 	this software and associated documentation files (the "Software"), to deal in
// 	the Software without restriction, including without limitation the rights to
// 	use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// 	the Software, and to permit persons to whom the Software is furnished to do so,
// 	subject to the following conditions:
// 
// 	The above copyright notice and this permission notice shall be included in all
// 	copies or substantial portions of the Software.
// 
// 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// 	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// 	FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// 	COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// 	IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// 	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// */

#endregion

using System;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using Xunit;
using Elasticsearch.Net;
using System.Threading;

namespace ElasticIdentity.Tests
{
	public class ElasticUserStoreTests
	{
        const string ElasticServerUrl = "http://localhost:9200";
        const string UserId = "1";
        const string UserName = "testuser";

        [Fact]
		public async Task CreateUserWithOwnId()
		{
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser(UserId, UserName)
                {
                    Phone = new ElasticUserPhone
                    {
                        Number = "555 123 1234",
                        IsConfirmed = true
                    },
                    Email = new ElasticUserEmail
                    {
                        Address = "hello@world.com",
                        IsConfirmed = false
                    }
                };

                await store.CreateAsync(user);

                var elasticUser = await store.FindByNameAsync(UserName);
                Assert.NotNull(elasticUser);
                Assert.Equal(UserId, elasticUser.Id);
                Assert.Equal(UserName, elasticUser.UserName);
                Assert.NotNull(elasticUser.Version);
            }
		}

        [Fact]
        public async Task CreateUserWithElasticId()
        {
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser()
                {
                    UserName = UserName,
                    Phone = new ElasticUserPhone
                    {
                        Number = "555 123 1234",
                        IsConfirmed = true
                    },
                    Email = new ElasticUserEmail
                    {
                        Address = "hello@world.com",
                        IsConfirmed = false
                    }
                };

                await store.CreateAsync(user);

                var elasticUser = await store.FindByNameAsync(UserName);
                Assert.NotNull(elasticUser);
                Assert.Equal(elasticUser.UserName, UserName);
                Assert.NotNull(elasticUser.Id);
                Assert.NotEqual(UserId, elasticUser.Id);
                Assert.NotNull(elasticUser.Version);
            }
        }

        [Fact]
        public async Task FailToCreateUserDueToExistingId()
        {
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser(UserId, UserName);

                await store.CreateAsync(user);

                bool threwConflictException = false;
                try
                {
                    await store.CreateAsync(user);
                }
                catch (ElasticsearchClientException ex)
                {
                    if (ex.Message.Contains("409"))
                    {
                        threwConflictException = true;
                    }
                }

                Assert.True(threwConflictException);
            }
        }

        [Fact]
		public async Task FindById()
		{
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser(UserId, UserName);

                await store.CreateAsync(user);

                var elasticUser = await store.FindByIdAsync(user.Id);

                Assert.NotNull(elasticUser);
                Assert.Equal(user.Id, elasticUser.Id);
            }
		}

		[Fact]
		public async Task UserNotFoundShouldReturnNullWhenThrowExceptionsOff()
		{
            using (var store = new UserStoreFixture<ElasticUser>(ElasticServerUrl, "elasticidentity-tests", true, false))
            {
                var user404 = await With503RetryForIndexRecovery(async () => await store.FindByIdAsync("missing"), 5);

                Assert.Null(user404);
            }
		}

        [Fact]
        public async Task UserNotFoundShouldReturnNullWhenThrowExceptionsOn()
        {
            using (var store = new UserStoreFixture<ElasticUser>(ElasticServerUrl, "elasticidentity-tests", true, true))
            {
                var user404 = await With503RetryForIndexRecovery(async () => await store.FindByIdAsync("missing"), 5);

                Assert.Null(user404);
            }
        }

        private async Task<ElasticUser> With503RetryForIndexRecovery(Func<Task<ElasticUser>> func, int maxRetries)
        {
            // every test starts by recreating an index in a lazy fashion
            // between the time the index gets to a post recovery stage and the query comes in, 503 errors might be returned

            if (maxRetries <= 0) maxRetries = 1;
            var attempt = 0;

            while (attempt <= maxRetries)
            {
                attempt++;

                try
                {
                    return await func();
                }
                catch (ElasticsearchClientException ex)
                {
                    if (ex.Response.HttpStatusCode != 503 || attempt == maxRetries)
                    {
                        throw;
                    }

                    Thread.Sleep(1000);
                }
            }

            return null;
        }

        [Fact]
        public async Task FindByName()
        {
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser(UserId, UserName)
                {
                    Email = new ElasticUserEmail
                    {
                        Address = "hello@world.com",
                        IsConfirmed = false
                    }
                };

                await store.CreateAsync(user);

                var elasticUser = await store.FindByNameAsync(UserName);

                Assert.NotNull(elasticUser);
                Assert.Equal(user.UserName, elasticUser.UserName);

                // should ignore case
                elasticUser = await store.FindByNameAsync(UserName.ToUpper());

                Assert.NotNull(elasticUser);
                Assert.Equal(user.UserName, elasticUser.UserName);
            }
        }

        [Fact]
		public async Task FindByEmail()
		{
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser(UserId, UserName)
                {
                    Email = new ElasticUserEmail
                    {
                        Address = "hello@WORLD.com",
                        IsConfirmed = false
                    }
                };

                await store.CreateAsync(user);

                var elasticUser = await store.FindByEmailAsync(user.Email.Address);

                Assert.NotNull(elasticUser);
                Assert.Equal(user.EmailAddress, elasticUser.EmailAddress);

                // should ignore case
                elasticUser = await store.FindByEmailAsync(user.Email.Address.ToUpper());

                Assert.NotNull(elasticUser);
                Assert.Equal(user.EmailAddress, elasticUser.EmailAddress);
            }
		}

		[Fact]
		public async Task DeleteUser()
		{
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser(UserId, UserName);

                await store.CreateAsync(user);

                await store.DeleteAsync(user);

                var elasticUser = await store.FindByNameAsync(user.UserName);
                Assert.Null(elasticUser);
            }
		}

		[Fact]
		public async Task UpdateUser()
		{
            using (var store = new UserStoreFixture<ElasticUser>())
            {
                var user = new ElasticUser(UserId, UserName);

                await store.CreateAsync(user);

                user = await store.FindByIdAsync(user.Id);
                user.Roles.Add("hello");

                await store.UpdateAsync(user);
                user = await store.FindByIdAsync(user.Id);

                Assert.True(user.Roles.Contains("hello"));

                // create another user object from the same id
                var sameUser = await store.FindByIdAsync(user.Id);
                sameUser.Roles.Add("another_role");
                await store.UpdateAsync(sameUser);
                sameUser = await store.FindByIdAsync(sameUser.Id);

                Assert.True(sameUser.Roles.Contains("another_role"));

                // same id, different versions
                Assert.Equal(user.Id, sameUser.Id);
                Assert.NotEqual(user.Version, sameUser.Version);

                // exception should be thrown as we're attempting to
                // update the original, out of date, user.
                user.Roles.Add("bad_role");
                await Assert.ThrowsAsync<Elasticsearch.Net.ElasticsearchClientException>(async () => await store.UpdateAsync(user));
            }
		}

        [Fact]
        public async Task CustomIndexAndTypeName()
        {
            var indexName = "custom-index";

            using (var store = new UserStoreFixture<ExtendedUser>(ElasticServerUrl, indexName, true, true))
            {
                var user = new ExtendedUser(UserId, UserName);
                
                user.Roles.UnionWith(new[] { "hello" });

                await store.CreateAsync(user);

                var response = store.ElasticClient.Get<ExtendedUser>(new GetRequest(indexName, TypeName.From<ExtendedUser>(), user.Id));

                Assert.NotNull(response.Source);
                Assert.Equal(response.Source.UserName, user.UserName);
            }
        }

        [Fact]
        public async Task UserWithExtendedProperties()
        {
            using (var store = new UserStoreFixture<ExtendedUser>())
            {
                await store.CreateAsync(new ExtendedUser("abc123", "abc123")
                {
                    Car = new Tesla
                    {
                        LicensePlate = "ABC123",
                        Model = TeslaModel.ModelS
                    }
                });

                await store.CreateAsync(new ExtendedUser("def456", "def456")
                {
                    Car = new Koenigsegg
                    {
                        LicensePlate = "ABC123",
                        Model = KoenigseggModel.One
                    }
                });

                var users = await store.GetAllAsync();

                var teslaUser = users.FirstOrDefault(x => x.UserName == "abc123");
                var koenigseggUser = users.FirstOrDefault(x => x.UserName == "def456");

                Assert.NotNull(teslaUser);
                Assert.NotNull(koenigseggUser);

                Assert.IsType<Tesla>((teslaUser as ExtendedUser).Car);
                Assert.IsType<Koenigsegg>((koenigseggUser as ExtendedUser).Car);
            }
        }

        class UserStoreFixture<TUser> : ElasticUserStore<TUser> where TUser : ElasticUser
        {
            public UserStoreFixture()
                : this(ElasticServerUrl, "elasticidentity-tests", true, true)
            {
            }

            public UserStoreFixture(string url, string index, bool forceRecreate, bool throwExceptions)
                : base(new ElasticClient(new ConnectionSettings(new Uri(url))
                    .MapDefaultTypeIndices(x => x.Add(typeof(TUser), index))
                    .ThrowExceptions(throwExceptions)
                    .DisableAutomaticProxyDetection(false)), index, forceRecreate)
            {
                Index = index;
            }

            public IElasticClient ElasticClient
            {
                get
                {
                    return Client;
                }
            }

            public string Index { get; private set; }

            public override void Dispose()
            {
                if (Client != null && IndexCreated)
                {
                    Client.DeleteIndex(new DeleteIndexRequest(Indices.Index(Index)));
                }
            }
        }
	}
}