#region MIT License

/*
	The MIT License (MIT)

	Copyright (c) 2013 Bombsquad Inc

	Permission is hereby granted, free of charge, to any person obtaining a copy of
	this software and associated documentation files (the "Software"), to deal in
	the Software without restriction, including without limitation the rights to
	use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
	the Software, and to permit persons to whom the Software is furnished to do so,
	subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
	FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
	COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
	IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.AspNet.Identity;
using Nest;

namespace ElasticIdentity
{
    public class ElasticUserStore<TUser> :
        ElasticUserStore,
        IUserStore<TUser>,
        IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserRoleStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserTwoFactorStore<TUser, string>,
        IUserEmailStore<TUser, string>,
        IUserPhoneNumberStore<TUser, string>
        where TUser : ElasticUser
    {
        private readonly Lazy<IElasticClient> _client;

        protected virtual IElasticClient Client
        {
            get
            {
                return _client.Value;
            }
        }

        protected virtual bool IndexCreated
        {
            get; set;
        }

        public ElasticUserStore(Uri elasticServerUri, string indexName = "users", bool forceRecreate = false)
        {
            if (elasticServerUri == null)
            {
                throw new ArgumentNullException(nameof(elasticServerUri));
            }

            _client = new Lazy<IElasticClient>(() =>
            {
                // most basic client settings, for everything else use the constructor that takes IElastiClient
                var settings = new ConnectionSettings(elasticServerUri)
                    .MapDefaultTypeIndices(x => x.Add(typeof(TUser), indexName));

                var client = new ElasticClient(settings);

                // TODO: move the setup logic out of the store, the store should not be concerned with db setup
                // see other providers, e.g. entity framework, mongo for reference
                // instead provide a class that will facilitate index setup as part of the package
                // this will clean up this code quite a bit and keep the tests easier to manage from the standpoint of setup and teardown                
                EnsureIndex(client, indexName, forceRecreate);
                return client;
            });
        }

        public ElasticUserStore(IElasticClient client, string indexName = "users", bool forceRecreate = false)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = new Lazy<IElasticClient>(() =>
            {
                EnsureIndex(client, indexName, forceRecreate);
                return client;
            });
        }

        protected virtual void EnsureIndex(IElasticClient connection, string indexName, bool forceCreate)
        {
            var exists = Wrap(connection.IndexExists(new IndexExistsRequest(indexName))).Exists;

            if (exists && forceCreate)
            {
                Wrap(connection.DeleteIndex(new DeleteIndexRequest(indexName)));
                exists = false;
            }

            if (!exists)
            {
                var response = Wrap(connection.CreateIndex(indexName, DescribeIndex));

                IndexCreated = AssertResponseSuccess(response);
            }
        }

        public static ICreateIndexRequest DescribeIndex(CreateIndexDescriptor createIndexDescriptor)
        {
            return createIndexDescriptor.Settings(s => s
                .Analysis(a => a
                    .Analyzers(aa => aa
                        .Custom("lowercaseKeyword", c => c
                        .Tokenizer("keyword")
                        .Filters("standard", "lowercase")))))
                .Mappings(m => m
                    .Map<TUser>(mm => mm
                        .AutoMap()
                        .AllField(af => af
                            .Enabled(false))));
        }

        private bool AssertResponseSuccess(IResponse response)
        {
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
            else
            {
                if (!response.ApiCall.Success)
                {
                    throw new Exception($"Error while creating index:\n{response.DebugInformation}");
                }
            }

            return true;
        }

        public virtual void Dispose()
        {
        }

        private async Task CreateOrUpdateAsync(TUser user, bool create)
        {
            if (user == null) throw new ArgumentNullException("user");
            
            // We need to specify op_type as we are generating the ID (guid) in code.
            // https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-index_.html
            // https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-index_.html#operation-type
            // On versioning.
            // https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-index_.html#index-versioning

            if (create)
                Wrap(await Client.IndexAsync(user, x => x
                  .OpType(OpType.Create)      // Fail if a document with the ID provided already exists.
                  .Consistency(Consistency.Quorum)
                  .Refresh()));
            else
                Wrap(await Client.IndexAsync(user, x => x
                  .OpType(OpType.Index)
                  .Version(user.Version)    // Be sure the document's version hasn't changed.
                  .Consistency(Consistency.Quorum)
                  .Refresh()));
        }

        public Task CreateAsync(TUser user)
        {
            return CreateOrUpdateAsync(user, true);
        }

        public Task UpdateAsync(TUser user)
        {
            return CreateOrUpdateAsync(user, false);
        }

        public async Task DeleteAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            Wrap(await Client.DeleteAsync(DocumentPath<TUser>.Id(user.Id), d => d
            .Consistency(Consistency.Quorum)
            .Version(user.Version)
            .Refresh()));
        }

        public async Task<TUser> FindByIdAsync(string userId)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));
            var result = Wrap(await Client.GetAsync(DocumentPath<TUser>.Id(userId)));

            if (!result.IsValid || !result.Found)
                return null;

            var r = result.Source;
            r.Version = result.Version;
            r.Id = result.Id;
            return r;
        }

        public async Task<TUser> FindByNameAsync(string userName)
        {
            if (userName == null)
                throw new ArgumentNullException(nameof(userName));

            var result = Wrap(await Client.SearchAsync<TUser>(s => s
              .Version(true)      // Bug: This is default, but we only get version if we set this. Need to file.
              .Query(q => q
                 .Bool(b => b
                    .Filter(f => f
                       .Term(t => t
                          .Field(tf => tf.UserName)
                          .Value(userName.ToLowerInvariant())))))));

            if (!result.IsValid || result.TerminatedEarly || result.TimedOut || !result.Documents.Any())
                return null;

            var r = result.Documents.FirstOrDefault();

            if (r == null)
                return null;

            // ToDo: Fix these
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once PossibleInvalidOperationException
            r.Version = result.Hits.FirstOrDefault().Version.Value;
            r.Id = result.Hits.FirstOrDefault()?.Id;

            return r;
        }

        public async Task<TUser> FindByEmailAsync(string email)
        {
            if (email == null) throw new ArgumentNullException(nameof(email));
            
            var result = Wrap(await Client.SearchAsync<TUser>(s => s
              .Version(true)      // Bug: This is default, but we only get version if we set this. Need to file.
              .Query(q => q
                 .Bool(b => b
                    .Filter(f => f
                       .Term(t => t
                          .Field(tf => tf.Email.Address)
                          .Value(email)))))));

            if (!result.IsValid || result.TerminatedEarly || result.TimedOut || !result.Documents.Any())
                return null;

            var r = result.Documents.FirstOrDefault();

            if (r == null)
                return null;

            // ToDo: Fix these
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once PossibleInvalidOperationException
            r.Version = result.Hits.FirstOrDefault().Version.Value;
            r.Id = result.Hits.FirstOrDefault()?.Id;

            return r;
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            user.Logins.Add(new ElasticUserLoginInfo
            {
                LoginProvider = login.LoginProvider,
                ProviderKey = login.ProviderKey
            });
            return DoneTask;
        }

        public Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");
            user.Logins.RemoveAll(x => x.LoginProvider == login.LoginProvider && x.ProviderKey == login.ProviderKey);
            return DoneTask;
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return Task.FromResult<IList<UserLoginInfo>>(user
                .Logins
                .Select(x => new UserLoginInfo(x.LoginProvider, x.ProviderKey))
                .ToList());
        }

        public async Task<TUser> FindAsync(UserLoginInfo login)
        {
            if (login == null) throw new ArgumentNullException(nameof(login));
            
            var result = Wrap(await Client.SearchAsync<TUser>(s => s
              .Query(q => q
                 .Bool(b => b
                    .Filter(f =>
                       f.Term(t1 => t1
                          .Field(tf1 => tf1.Logins.First().LoginProvider)
                          .Value(login.LoginProvider))
                       &&
                       f.Term(t2 => t2
                          .Field(tf2 => tf2.Logins.First().ProviderKey)
                          .Value(login.ProviderKey)))))));

            if (!result.IsValid || result.TerminatedEarly || result.TimedOut || !result.Documents.Any())
                return null;

            var r = result.Documents.FirstOrDefault();

            if (r == null)
                return null;

            // ToDo: Fix these
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once PossibleInvalidOperationException
            r.Version = result.Hits.FirstOrDefault().Version.Value;
            r.Id = result.Hits.FirstOrDefault()?.Id;
            return r;
        }

        public Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var result = (IList<Claim>)user
                .Claims
                .Select(x => x.AsClaim())
                .ToList();
            return Task.FromResult(result);
        }

        public Task AddClaimAsync(TUser user, Claim claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");
            user.Claims.Add(claim);
            return DoneTask;
        }

        public Task RemoveClaimAsync(TUser user, Claim claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");
            user.Claims.Remove(claim);
            return DoneTask;
        }

        public Task AddToRoleAsync(TUser user, string role)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (role == null) throw new ArgumentNullException("role");
            user.Roles.Add(role);
            return DoneTask;
        }

        public Task RemoveFromRoleAsync(TUser user, string role)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (role == null) throw new ArgumentNullException("role");
            user.Roles.Remove(role);
            return DoneTask;
        }

        public Task<IList<string>> GetRolesAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var result = user.Roles.ToList();
            return Task.FromResult((IList<string>)result);
        }

        public Task<bool> IsInRoleAsync(TUser user, string role)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (role == null) throw new ArgumentNullException("role");
            return Task.FromResult(user.Roles.Contains(role));
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            if (user == null) throw new ArgumentNullException("user");
            user.PasswordHash = passwordHash;
            return DoneTask;
        }

        public Task<string> GetPasswordHashAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return Task.FromResult(user.PasswordHash != null);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp)
        {
            if (user == null) throw new ArgumentNullException("user");
            user.SecurityStamp = stamp;
            return DoneTask;
        }

        public Task<string> GetSecurityStampAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return Task.FromResult(user.SecurityStamp);
        }

        public async Task<IEnumerable<TUser>> GetAllAsync()
        {
            // ToDo: Use scroll -> https://goo.gl/E86ezB
            // Due to the nature Elasticsearch allocates memory on the JVM heap for use in storing the results
            // you should not set the size to a very large value. Relying on the default size of 10 for now.
            var result = Wrap(await Client.SearchAsync<TUser>(search => search.MatchAll()));
            return result.Documents;
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            if (user == null) throw new ArgumentNullException("user");
            user.TwoFactorAuthenticationEnabled = enabled;
            return DoneTask;
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return Task.FromResult(user.TwoFactorAuthenticationEnabled);
        }

        public Task SetEmailAsync(TUser user, string email)
        {
            if (user == null) throw new ArgumentNullException("user");
            user.Email = email == null
                ? null
                : new ElasticUserEmail { Address = email };
            return DoneTask;
        }

        public Task<string> GetEmailAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var elasticUserEmail = user.Email;

            return elasticUserEmail != null
                ? Task.FromResult(elasticUserEmail.Address)
                : Task.FromResult<string>(null);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var elasticUserEmail = user.Email;

            return elasticUserEmail != null
                ? Task.FromResult(elasticUserEmail.IsConfirmed)
                : Task.FromResult(false);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            if (user == null) throw new ArgumentNullException("user");
            var elasticUserEmail = user.Email;
            if (elasticUserEmail != null)
                elasticUserEmail.IsConfirmed = true;
            else throw new InvalidOperationException("User have no configured email address");
            return DoneTask;
        }

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            if (user == null) throw new ArgumentNullException("user");
            user.Phone = phoneNumber == null
                ? null
                : new ElasticUserPhone { Number = phoneNumber };
            return DoneTask;
        }

        public Task<string> GetPhoneNumberAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var elasticUserPhone = user.Phone;

            return elasticUserPhone != null
                ? Task.FromResult(elasticUserPhone.Number)
                : Task.FromResult<string>(null);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var elasticUserPhone = user.Phone;

            return elasticUserPhone != null
                ? Task.FromResult(elasticUserPhone.IsConfirmed)
                : Task.FromResult(false);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            if (user == null) throw new ArgumentNullException("user");
            var elasticUserPhone = user.Phone;
            if (elasticUserPhone != null)
                elasticUserPhone.IsConfirmed = true;
            else throw new InvalidOperationException("User have no configured phone number");
            return DoneTask;
        }
    }

    public abstract class ElasticUserStore
    {
        protected static readonly Task DoneTask = Task.FromResult(true);
        protected const int DefaultSizeForAll = 1000 * 1000;

        public event EventHandler<ElasticUserStoreTraceEventArgs> Trace;

        protected virtual void OnTrace(IApiCallDetails callDetails)
        {
            var trace = Trace;
            trace?.Invoke(this, new ElasticUserStoreTraceEventArgs(callDetails.DebugInformation));
        }

        protected T Wrap<T>(T result) where T : IResponse
        {
            var c = result.ApiCall;
            OnTrace(c);
            return result;
        }
    }
}