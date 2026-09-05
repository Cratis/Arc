// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_schema_request;

public class and_provider_implements_generic_interface : given.an_identity_schema_endpoint_handler
{
    void Establish()
    {
        _services.AddSingleton<IProvideIdentityDetails>(new TypedIdentityDetailsProvider());
        MapIdentityProviderEndpoint();
    }

    async Task Because() => await _mappedHandlers["/.cratis/identity-details/schema"](_httpRequestContext);

    [Fact]
    void should_write_schema_for_typed_details()
    {
        _httpRequestContext.Received(1).WriteResponseAsJson(
            Arg.Is<object>(value => value != null && value.ToString()!.Contains("specialField")),
            Arg.Any<Type>(),
            CancellationToken.None);
    }

    record TypedDetails(string SpecialField);

    class TypedIdentityDetailsProvider : IProvideIdentityDetails<TypedDetails>
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new TypedDetails("value")));
    }
}
