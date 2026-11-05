// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2ServiceCollectionExtensions;

public class when_resolving_two_scopes : Specification
{
    bool _sameNonceStore;
    IForwardedIdentityNonceStore _store;
    bool _differentVerifiers;
    CredentialVerification _first;
    CredentialVerification _second;
    CredentialVerification _disabled;
    bool _registered;
    ForwardedIdentityV2Rejection? _secondCore;

    async Task Because()
    {
        var vector = VerificationFixture.DirectBrowser();
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        var request = VerificationFixture.RawRequest(vector);
        requests.Current.Returns(request);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(requests);
        services.AddSingleton(new VerifiedCallerInstallation("resource", "test"));
        services.AddSingleton<ICallerPersonDirectory>(new PersonDirectory());
        services.AddSingleton<TimeProvider>(new FixedTime(vector.Inputs.Now));
        services.AddProcessLocalForwardedIdentityNonceStore();
        services.AddForwardedIdentityV2(VerificationFixture.Settings(vector.Inputs));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();
        _store = scopeA.ServiceProvider.GetRequiredService<IForwardedIdentityNonceStore>();
        _registered = scopeA.ServiceProvider.GetRequiredService<IOptions<ArcOptions>>().Value.ForwardedIdentityV2;
        _sameNonceStore = ReferenceEquals(scopeA.ServiceProvider.GetRequiredService<IForwardedIdentityNonceStore>(), scopeB.ServiceProvider.GetRequiredService<IForwardedIdentityNonceStore>());
        _differentVerifiers = !ReferenceEquals(scopeA.ServiceProvider.GetRequiredService<ForwardedIdentityV2Verifier>(), scopeB.ServiceProvider.GetRequiredService<ForwardedIdentityV2Verifier>());
        _first = await scopeA.ServiceProvider.GetServices<IVerifiedCredentialSource>().Single().Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
        _second = await scopeB.ServiceProvider.GetServices<IVerifiedCredentialSource>().Single().Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
        _secondCore = (await scopeB.ServiceProvider.GetRequiredService<ForwardedIdentityV2Verifier>().Verify(VerificationFixture.Settings(vector.Inputs), VerificationFixture.Request(vector), CancellationToken.None)).Rejection;
        request.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value.TrustForwardedIdentityHeaders = false;
        _disabled = await scopeA.ServiceProvider.GetServices<IVerifiedCredentialSource>().Single().Verify(VerificationFixture.Presentation(vector), CancellationToken.None);
    }

    [Fact] void should_mark_v2_registration_in_arc_options() => _registered.ShouldBeTrue();
    [Fact] void should_ignore_v2_with_the_master_switch_off() => _disabled.Applicable.ShouldBeFalse();
    [Fact] void should_use_process_local_storage_only_after_explicit_opt_in() => _store.ShouldBeOfExactType<ProcessLocalForwardedIdentityNonceStore>();
    [Fact] void should_share_one_process_wide_nonce_store() => _sameNonceStore.ShouldBeTrue();
    [Fact] void should_keep_verifiers_scoped() => _differentVerifiers.ShouldBeTrue();
    [Fact] void should_accept_in_scope_a() => _first.Facts.ShouldNotBeNull();
    [Fact] void should_refuse_in_scope_b() => _second.Refusal.ShouldEqual(CallerRefusal.SourceRefused);
    [Fact] void should_refuse_the_same_nonce_as_replay_across_scopes() => _secondCore.ShouldEqual(ForwardedIdentityV2Rejection.Replay);
}
