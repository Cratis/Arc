// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens.given;

public class a_pat_module : Specification
{
    internal static readonly DateTimeOffset Now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    internal readonly TenantId _tenant = new("tenant-one");
    internal readonly CallerAccountKey _account = new("https://identity.example", "8b167ad2-0f89-4c78-9d42-6d4580495d32");
    internal readonly CallerPersonId _person = new("person-one");
    internal IPatAuthority _authority;
    internal IPatRecoveryEpoch _epochs;
    internal IPatPolicy _policy;
    internal IPatIssuanceContext _context;
    internal PatSettings _settings;
    internal PersonalAccessTokens _tokens;
    internal PatRecord? _record;
    internal PatIssuedIntent? _intent;
    internal VerifiedCaller _caller;
    internal TimeProvider _clock;
    internal int _policyCalls;
    internal int _creates;
    internal bool _revoked;

    void Establish()
    {
        _settings = PatSettings.Create(new("direct") { AllowedScopes = ["read"] }, new("resource-a", "https://resource-a.example", "test"));
        _clock = Substitute.For<TimeProvider>();
        _clock.GetUtcNow().Returns(Now);
        _authority = Substitute.For<IPatAuthority>();
        _epochs = Substitute.For<IPatRecoveryEpoch>();
        _epochs.Read(Arg.Any<CancellationToken>()).Returns(4UL);
        _policy = Substitute.For<IPatPolicy>();
        _policy.Evaluate(Arg.Any<PatRecord>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _policyCalls++;
            return new PatPolicyDecision(true, TimeSpan.FromDays(90), 7);
        });
        _authority.TryCreate(Arg.Any<PatRecord>(), Arg.Any<PatIssuedIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _record = call.Arg<PatRecord>();
            _intent = call.Arg<PatIssuedIntent>();
            _creates++;
            return true;
        });
        _authority.ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => _record is null ? null : _record with { Revoked = _revoked });
        _authority.TryRevoke(Arg.Any<PatOwner>(), Arg.Any<CredentialId>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (_record is null || call.Arg<PatOwner>() != new PatOwner(_record.Resource, _record.Environment, _record.Person, _record.AccountKey, _record.Tenant)) return PatRevokeResult.Refused;
            if (_revoked) return PatRevokeResult.AlreadyRevoked;
            _revoked = true;
            return PatRevokeResult.Revoked;
        });
        _context = Substitute.For<IPatIssuanceContext>();
        _caller = new VerifiedCallerBuilder(new(new Dictionary<string, string> { ["resource-a"] = "https://resource-a.example" })).Build(
            new(CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new("session-one"), _settings.Target)
            {
                AccountKey = _account,
                Person = _person,
                Tenant = _tenant,
                Assurance = new("https://upstream.example", Now.AddMinutes(-5).ToUnixTimeSeconds(), null, ["sso"]),
                Scopes = new HashSet<string> { "read" }
            },
            new("resource-a", "test"),
            _tenant).Caller!;
        _context.GetCurrent(Arg.Any<CancellationToken>()).Returns(_ => _caller);
        _tokens = new(_settings, _authority, _epochs, _policy, _context, _clock);
    }

    public class PersonDirectory : ICallerPersonDirectory
    {
        public Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken) => Task.FromResult(CallerPersonFound.NotFound());
    }

    internal PatCredentialSource Source() => new(_settings, _authority, _epochs, _policy, _clock);

    internal static CredentialPresentation Presentation(string token) => new(["Bearer " + token], new Dictionary<string, IReadOnlyList<string>>(), shape: PresentedCredentialShape.PatBearer);

    internal async Task<string> Issue()
    {
        var result = await _tokens.Issue(TimeSpan.FromDays(90), ["read"], TestContext.Current.CancellationToken);
        return result.Token!.Reveal()!;
    }

    internal IVerifiedCallerAccessor Resolver(string authorization, IEnumerable<string>? markers = null, IVerifiedCredentialSource? oauth = null, IVerifiedCredentialSource? pat = null, IEnumerable<string>? reserved = null)
    {
        var request = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest, ICanPreserveCallerCredentialHeaders>();
        request.Items.Returns(new Dictionary<object, object?>());
        ((ICanPreserveCallerCredentialHeaders)request).PreservesDuplicateCredentialHeaders.Returns(true);
        var raw = (ICanReadForwardedIdentityRequest)request;
        raw.RawTarget.Returns("/mcp");
        raw.HeaderNames.Returns(new[] { "Authorization" }.Concat(markers ?? []));
        raw.GetHeaderValues(Arg.Any<string>()).Returns(["untrusted-marker"]);
        raw.GetHeaderValues("Authorization").Returns([authorization]);
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(request);
        var tenants = Substitute.For<ITenantIdAccessor>();
        tenants.Current.Returns(_tenant);
        return new VerifiedCallerResolver(
            requests,
            oauth is null ? [pat ?? Source()] : [pat ?? Source(), oauth],
            new(new(reserved ?? ["dpat_"])),
            new(new(new Dictionary<string, string> { ["resource-a"] = "https://resource-a.example" })),
            new("resource-a", "test"),
            tenants);
    }
}
