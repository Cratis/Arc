// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolutionOptions;

public class when_nothing_is_configured : Specification
{
    [Fact] void should_not_be_configured() => new ConnectionResolutionOptions().IsConfigured.ShouldBeFalse();
    [Fact] void should_not_authenticate_the_http_resolver() => new ConnectionResolutionOptions().Identity.Http.Authentication.Type.ShouldEqual(HttpConnectionResolverAuthenticationType.None);
    [Fact] void should_use_a_managed_identity_for_azure() => new HttpConnectionResolverAuthenticationOptions().Credential.ShouldEqual(AzureCredentialType.ManagedIdentity);
    [Fact] void should_refresh_tokens_five_minutes_early() => new HttpConnectionResolverAuthenticationOptions().RefreshMargin.ShouldEqual(TimeSpan.FromMinutes(5));
}
