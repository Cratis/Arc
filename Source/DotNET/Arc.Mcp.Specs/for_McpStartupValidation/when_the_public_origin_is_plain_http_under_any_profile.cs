// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_the_public_origin_is_plain_http_under_any_profile : a_startup_validation
{
    readonly List<string> _rejected = [];
    readonly List<string> _accepted = [];
    bool _allowed;

    void Because()
    {
        foreach (var profile in new[] { McpProfile.PatOnly, McpProfile.OAuthOnly, McpProfile.Combined })
        {
            Run(profile, "http://public.example");
            Run(profile, "https://public.example/base");
            Run(profile, "http://localhost:5000");
        }

        _services.Clear();
        Run(McpProfile.PatOnly, null);
        _allowed = McpOrigins.Allows(["http://public.example"], new McpOptions { PublicOrigin = "http://public.example" });
    }

    [Fact] void should_reject_every_non_origin_public_origin() => _rejected.Count.ShouldEqual(6);
    [Fact] void should_accept_loopback_and_unset_origins() => _accepted.Count.ShouldEqual(4);
    [Fact] void should_not_allow_the_plain_http_origin() => _allowed.ShouldBeFalse();

    void Run(McpProfile profile, string? origin)
    {
        _services.Clear();
        _mcp = mcp => mcp.Configure(options =>
        {
            options.Profile = profile;
            options.PublicOrigin = origin;
            if (profile != McpProfile.PatOnly && origin is not null)
            {
                options.Resource = $"{origin}/mcp";
                options.AuthorizationServers.Add("https://issuer.example");
            }
        });
        Validate();
        (_error is null ? _accepted : _rejected).Add($"{profile}:{origin}");
    }
}
