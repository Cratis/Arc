// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.Azure.for_AzureCredentialAccessTokenProvider;

public class when_checking_whether_it_can_provide : Specification
{
    readonly AzureCredentialAccessTokenProvider _provider = new();

    [Fact] void should_provide_for_azure_credential() => _provider.CanProvide(new() { Type = HttpConnectionResolverAuthenticationType.AzureCredential }).ShouldBeTrue();
    [Fact] void should_not_provide_for_client_credentials() => _provider.CanProvide(new() { Type = HttpConnectionResolverAuthenticationType.ClientCredentials }).ShouldBeFalse();
}
