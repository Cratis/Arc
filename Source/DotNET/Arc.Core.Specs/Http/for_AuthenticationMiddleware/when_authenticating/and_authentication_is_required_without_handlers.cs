// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class and_authentication_is_required_without_handlers : given.an_authentication_middleware
{
    Exception? _failure;

    void Establish()
    {
        _authentication.HasHandlers.Returns(false);
        _metadata = new EndpointMetadata("ProtectedEndpoint") { RequireAuthentication = true };
    }

    async Task Because() => _failure = await Catch.Exception(() => _middleware.Authenticate(_httpRequestContext, _metadata));

    [Fact] void should_throw_the_generic_authentication_configuration_error() => _failure.ShouldBeOfExactType<AuthenticationRequiredWithoutHandlers>();
    [Fact] void should_name_the_protected_endpoint() => _failure!.Message.ShouldContain("ProtectedEndpoint");
}
