// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens;

[PerFactSpecificationLifecycle]
public class when_an_oauth_grant_requests_issuance : given.a_pat_module
{
    void Establish() => _caller = _caller with { CredentialKind = CallerCredentialKind.OAuthGrant };

    [Fact]
    public async Task should_refuse_inside_the_oauth_pat_composition_before_policy_or_persistence()
    {
        var result = await IssueFromOAuth(true);
        using var token = result.Token;
        token.ShouldBeNull();
        result.RefusalCode.ShouldEqual("pat_issuance_refused");
        _policyCalls.ShouldEqual(0);
        _creates.ShouldEqual(0);
    }

    [Fact]
    public async Task should_preserve_issuance_without_the_oauth_pat_composition()
    {
        var result = await IssueFromOAuth(false);
        using var token = result.Token;
        token.ShouldNotBeNull();
        result.RefusalCode.ShouldBeNull();
        _creates.ShouldEqual(1);
    }

    async Task<PatIssueResult> IssueFromOAuth(bool composition)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_settings);
        services.AddSingleton(_authority);
        services.AddSingleton(_epochs);
        services.AddSingleton(_policy);
        services.AddSingleton(_context);
        services.AddSingleton(_clock);
        services.AddScoped<IPersonalAccessTokens, PersonalAccessTokens>();
        if (composition) services.AddSingleton(new OAuthCapableCallerInstallation());
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IPersonalAccessTokens>().Issue(TimeSpan.FromDays(1), ["read"], TestContext.Current.CancellationToken);
    }
}
