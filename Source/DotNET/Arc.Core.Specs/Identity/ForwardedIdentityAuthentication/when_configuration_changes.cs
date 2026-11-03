// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_configuration_changes : given.a_signed_request
{
    readonly Dictionary<string, ForwardedIdentityAuthenticationResult> _results = [];

    void Because()
    {
        Record("trust", () => _options.TrustForwardedIdentityHeaders = false);
        Record("audience", () => _options.ForwardedIdentity.Audience = "other");
        Record("empty audience", () => _options.ForwardedIdentity.Audience = string.Empty);
        Record("large audience", () => _options.ForwardedIdentity.Audience = new string('a', 129));
        Record("no keys", ClearKeys);
        Record("duplicate keys", () => _options.ForwardedIdentity.Keys.Add(_options.ForwardedIdentity.Keys[0]));
        Record("too many keys", () => { for (var index = 0; index < 8; index++) { _options.ForwardedIdentity.Keys.Add(new() { KeyId = $"k{index + 2}", SecretBase64 = Convert.ToBase64String(ForwardedIdentityForSpecs.Key) }); } });
        Record("key case", () => _options.ForwardedIdentity.Keys[0].KeyId = "K1");
        Record("bad key id", () => _options.ForwardedIdentity.Keys[0].KeyId = "bad.id");
        Record("short key", () => _options.ForwardedIdentity.Keys[0].SecretBase64 = Convert.ToBase64String(new byte[31]));
        Record("malformed key", () => _options.ForwardedIdentity.Keys[0].SecretBase64 = "not base64");
        Record("wrong key", () => _options.ForwardedIdentity.Keys[0].SecretBase64 = Convert.ToBase64String(new byte[32]));
        Record("inner audience", () => _options.OnBehalfOf.Audience = "changed");
        Record("inner issuer", () => _options.OnBehalfOf.Issuer = "changed");
        Record("inner service", () => _options.OnBehalfOf.Service = "changed");
        Record("inner enabled", () => _options.OnBehalfOf.Enabled = true);
        Record("inner skew", () => _options.OnBehalfOf.ClockSkew = TimeSpan.FromSeconds(1));
        Record("tenancy header", () => _options.Tenancy.HttpHeader = "other-tenant");
    }

    [Fact] void should_invalidate_every_relevant_configuration_snapshot() => _results.Where(pair => pair.Value.Principal is not null).Select(pair => pair.Key).ShouldBeEmpty();

    void ClearKeys() => _options.ForwardedIdentity.Keys.Clear();

    void Record(string name, Action mutate)
    {
        ResetRequest();
        Authenticate().Principal.ShouldNotBeNull();
        mutate();
        _results[name] = Authenticate();
    }
}
