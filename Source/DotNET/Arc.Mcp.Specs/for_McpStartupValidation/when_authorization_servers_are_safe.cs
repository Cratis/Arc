// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_authorization_servers_are_safe : a_startup_validation
{
    readonly Dictionary<string, Exception?> _errors = [];

    void Because()
    {
        foreach (var server in new[] { "https://issuer.example/tenant", "http://localhost:5000", "http://127.0.0.1:5000", "http://[::1]:5000" })
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

    [Fact] void should_accept_https_and_loopback_http() => _errors.Where(_ => _.Value is not null).Select(_ => _.Key).ShouldBeEmpty();
    [Fact] void should_check_every_case() => _errors.Count.ShouldEqual(4);
}
