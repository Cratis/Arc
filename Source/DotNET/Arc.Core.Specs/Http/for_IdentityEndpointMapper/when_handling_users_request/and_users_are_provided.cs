// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_users_request;

public class and_users_are_provided : given.an_identity_schema_endpoint_handler
{
    void Establish()
    {
        _usersProviders.GetEnumerator().Returns(_ => new List<ICanProvideUsers> { new FirstProvider(), new SecondProvider() }.GetEnumerator());
        MapIdentityProviderEndpoint();
    }

    async Task Because() => await _mappedHandlers["/.cratis/users"](_httpRequestContext);

    [Fact]
    void should_write_users_from_all_providers()
    {
        _httpRequestContext.Received(1).WriteResponseAsJson(
            Arg.Is<object>(value =>
                value != null &&
                ((IEnumerable<User>)value).Count() == 2 &&
                ((IEnumerable<User>)value).Any(_ => _.MicrosoftIdentity.UserId == "first") &&
                ((IEnumerable<User>)value).Any(_ => _.MicrosoftIdentity.UserId == "second")),
            typeof(IEnumerable<User>),
            CancellationToken.None);
    }

    class FirstProvider : ICanProvideUsers
    {
        public Task<IEnumerable<User>> Provide() =>
            Task.FromResult<IEnumerable<User>>(
            [
                new User(new ClientPrincipal { UserId = "first", UserDetails = "First User", IdentityProvider = "aad", UserRoles = [] }, new { IsDeveloper = true })
            ]);
    }

    class SecondProvider : ICanProvideUsers
    {
        public Task<IEnumerable<User>> Provide() =>
            Task.FromResult<IEnumerable<User>>(
            [
                new User(new ClientPrincipal { UserId = "second", UserDetails = "Second User", IdentityProvider = "aad", UserRoles = [] }, new { IsDeveloper = false })
            ]);
    }
}
