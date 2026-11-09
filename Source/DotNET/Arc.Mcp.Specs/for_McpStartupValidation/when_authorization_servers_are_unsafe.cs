// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_authorization_servers_are_unsafe : a_startup_validation
{
    readonly Dictionary<string, Exception?> _errors = [];

    void Because()
    {
        foreach (var server in new[]
        {
            "/relative", "http://issuer.example", "ftp://issuer.example", "https://user@issuer.example",
            "https://issuer.example?query", "https://issuer.example#fragment", "https://issuer.example?", "https://issuer.example#"
        })
        {
            _services.Clear();
            _mcp = mcp => mcp.Configure(options =>
            {
                options.Profile = McpProfile.Combined;
                options.PublicOrigin = "https://resource.example";
                options.Resource = "https://resource.example/mcp";
                options.AuthorizationServers.Add(server);
            });
            Validate();
            _errors[server] = _error;
        }
    }

    [Fact] void should_reject_every_unsafe_authorization_server() => _errors.Where(_ => _.Value is not McpOptionsInvalid).Select(_ => _.Key).ShouldBeEmpty();
    [Fact] void should_check_every_case() => _errors.Count.ShouldEqual(8);
}
