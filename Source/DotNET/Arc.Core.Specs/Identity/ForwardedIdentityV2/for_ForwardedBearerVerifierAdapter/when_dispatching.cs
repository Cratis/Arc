// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedBearerVerifierAdapter;

public class when_dispatching : Specification
{
    ForwardedBearerVerification _accepted;
    ForwardedBearerVerification _refusedPat;
    ForwardedBearerVerification _missing;
    ForwardedBearerVerification _duplicate;
    ForwardedBearerVerification _thrown;
    VerifiedCredentialFacts _facts;
    int _oauthCalls;
    CredentialPresentation _presentation;

    async Task Because()
    {
        var vector = ForwardedIdentityV2Vectors.Named("pat-bearer-caller-from-local-pat-verifier");
        var verified = await new VectorBearerVerifier(vector.Inputs.VerifiedBearer).Verify(ForwardedBearerVerifierKind.Pat, "Bearer fixture", CancellationToken.None);
        _facts = verified.Facts!;
        var pat = Substitute.For<IVerifiedCredentialSource>();
        pat.Shape.Returns(PresentedCredentialShape.PatBearer);
        pat.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _presentation = call.Arg<CredentialPresentation>();
            return Task.FromResult(CredentialVerification.Verified(_facts));
        });
        var oauth = Substitute.For<IVerifiedCredentialSource>();
        oauth.Shape.Returns(PresentedCredentialShape.OAuthBearer);
        oauth.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(CredentialVerification.Verified(_facts)));
        _accepted = await Verify([pat, oauth]);
        pat.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(CredentialVerification.Refused(CallerRefusal.SourceRefused)));
        _refusedPat = await Verify([pat, oauth]);
        _oauthCalls = oauth.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IVerifiedCredentialSource.Verify));
        _missing = await Verify([oauth]);
        _duplicate = await Verify([pat, pat]);
        pat.Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>()).Returns<Task<CredentialVerification>>(_ => throw new InvalidOperationException("Synthetic verifier failure."));
        _thrown = await Verify([pat, oauth]);
    }

    static async Task<ForwardedBearerVerification> Verify(IVerifiedCredentialSource[] sources)
    {
        var services = new ServiceCollection();
        foreach (var source in sources) services.AddScoped<IVerifiedCredentialSource>(_ => source);
        services.AddScoped<IForwardedBearerVerifier, ForwardedBearerVerifierAdapter>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IForwardedBearerVerifier>().Verify(ForwardedBearerVerifierKind.Pat, "Bearer fixture", CancellationToken.None);
    }

    [Fact] void should_preserve_the_verified_bearer_facts() => ReferenceEquals(_accepted.Facts, _facts).ShouldBeTrue();
    [Fact] void should_preserve_the_authorization_value() => _presentation.Authorization.ShouldEqual<string>("Bearer fixture");
    [Fact] void should_not_dispatch_an_envelope_to_the_bearer_source() => _presentation.Envelope.ShouldBeEmpty();
    [Fact] void should_dispatch_the_selected_shape() => _presentation.Shape.ShouldEqual(PresentedCredentialShape.PatBearer);
    [Fact] void should_refuse_pat_failure() => _refusedPat.IsAccepted.ShouldBeFalse();
    [Fact] void should_not_fallback_to_oauth() => _oauthCalls.ShouldEqual(0);
    [Fact] void should_refuse_missing_source() => _missing.IsAccepted.ShouldBeFalse();
    [Fact] void should_refuse_ambiguous_sources() => _duplicate.IsAccepted.ShouldBeFalse();
    [Fact] void should_fail_closed_on_verifier_exceptions() => _thrown.IsAccepted.ShouldBeFalse();
}
