// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.PersonalAccessTokens.for_PatCredentialScopeAllowlist;

public class when_installing_personal_access_tokens : Specification
{
    ServiceProvider _provider;
    ICredentialScopeAllowlist _snapshot;

    void Establish()
    {
        var scopes = new HashSet<string> { "app:read" };
        var services = new ServiceCollection();
        var builder = Substitute.For<IArcBuilder>();
        builder.Services.Returns(services);
        builder.WithPersonalAccessTokens(new("local") { AllowedScopes = scopes }, callers => callers.ForResource("resource", "audience", "Development").UsePersonDirectory<for_PersonalAccessTokens.given.a_pat_module.PersonDirectory>());
        scopes.Add("app:write");
        _provider = services.BuildServiceProvider();
    }

    void Because() => _snapshot = _provider.GetRequiredService<ICredentialScopeAllowlist>();

    [Fact] void should_identify_the_pat_credential_kind() => _snapshot.Kind.ShouldEqual(CallerCredentialKind.PersonalAccessToken);
    [Fact] void should_snapshot_the_configured_scopes() => _snapshot.Scopes.ShouldContainOnly("app:read");

    void Destroy() => _provider.Dispose();
}
