// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_accepting_equivalent_inputs : given.a_signed_request
{
    readonly Dictionary<string, ForwardedIdentityAuthenticationResult> _results = [];

    void Because()
    {
        Record("uppercase escape", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/base/a%2Fb?x=1"));
        Record("query not bound", () => ((ICanReadForwardedIdentityRequest)_context).RawTarget.Returns("/base/a%2fb?x=2"));
        Record("ows", () => _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = [" \tn\t "]);
        Record("lower edge", () => Resign(1699999940));
        Record("upper edge", () => Resign(1700000030));
        Record("comma in one name", () =>
        {
            _headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "Last, First";
            Resign();
        });
        Record("empty optional", () =>
        {
            _headers["tenant-id"] = string.Empty;
            Resign();
        });
        Record("custom tenancy", () =>
        {
            _options.Tenancy.HttpHeader = "custom-tenant";
            _headers["custom-tenant"] = "t";
            Resign();
        });
    }

    [Fact] void should_accept_the_documented_equivalences() => _results.Where(pair => pair.Value.Principal is null).Select(pair => pair.Key).ShouldBeEmpty();

    void Record(string name, Action mutate)
    {
        ResetRequest();
        mutate();
        _results[name] = Authenticate();
    }
}
