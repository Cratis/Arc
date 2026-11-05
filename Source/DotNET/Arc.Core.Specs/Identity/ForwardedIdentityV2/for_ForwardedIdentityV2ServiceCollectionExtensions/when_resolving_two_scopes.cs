// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2ServiceCollectionExtensions;

public class when_resolving_two_scopes : Specification
{
    bool _sameNonceStore;
    bool _differentVerifiers;
    CredentialVerification _first;
    CredentialVerification _second;
    ForwardedIdentityV2Rejection? _secondCore;

    async Task Because()
    {
        var vector = ForwardedIdentityV2Vectors.Named("credential-kind-browser-session");
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        var request = VerificationFixture.RawRequest(vector);
        requests.Current.Returns(request);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(requests);
        services.AddSingleton(new VerifiedCallerInstallation("resource", "test"));
        services.AddSingleton<ICallerPersonDirectory>(new PersonDirectory());
        services.AddSingleton<TimeProvider>(new FixedTime(vector.Inputs.Now));
        services.AddForwardedIdentityV2(VerificationFixture.Settings(vector.Inputs));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();
        _sameNonceStore = ReferenceEquals(scopeA.ServiceProvider.GetRequiredService<IForwardedIdentityNonceStore>(), scopeB.ServiceProvider.GetRequiredService<IForwardedIdentityNonceStore>());
        _differentVerifiers = !ReferenceEquals(scopeA.ServiceProvider.GetRequiredService<ForwardedIdentityV2Verifier>(), scopeB.ServiceProvider.GetRequiredService<ForwardedIdentityV2Verifier>());
        _first = await scopeA.ServiceProvider.GetServices<IVerifiedCredentialSource>().Single().Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
        _second = await scopeB.ServiceProvider.GetServices<IVerifiedCredentialSource>().Single().Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
        _secondCore = (await scopeB.ServiceProvider.GetRequiredService<ForwardedIdentityV2Verifier>().Verify(VerificationFixture.Settings(vector.Inputs), VerificationFixture.Request(vector), CancellationToken.None)).Rejection;
    }

    [Fact] void should_share_one_process_wide_nonce_store() => _sameNonceStore.ShouldBeTrue();
    [Fact] void should_keep_verifiers_scoped() => _differentVerifiers.ShouldBeTrue();
    [Fact] void should_accept_in_scope_a() => _first.Facts.ShouldNotBeNull();
    [Fact] void should_refuse_in_scope_b() => _second.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_refuse_the_same_nonce_as_replay_across_scopes() => _secondCore.ShouldEqual(ForwardedIdentityV2Rejection.Replay);
}
