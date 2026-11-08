// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_the_oauth_profile_is_missing_options : a_startup_validation
{
    List<Exception?> _errors;
    Exception? _complete;
    Exception? _patOnlyWithoutOptions;

    void Because()
    {
        _errors = [];
        foreach (var profile in new[] { McpProfile.OAuthOnly, McpProfile.Combined })
        {
            foreach (var missing in new[] { nameof(McpOptions.Resource), nameof(McpOptions.PublicOrigin), nameof(McpOptions.AuthorizationServers) })
            {
                _mcp = mcp => mcp.Configure(options => Configure(options, profile, missing));
                Reset();
                Validate();
                _errors.Add(_error);
            }
        }

        _mcp = mcp => mcp.Configure(options => Configure(options, McpProfile.Combined, null));
        Reset();
        Validate();
        _complete = _error;

        _mcp = mcp => mcp.Configure(options => options.Profile = McpProfile.PatOnly);
        Reset();
        Validate();
        _patOnlyWithoutOptions = _error;
    }

    static void Configure(McpOptions options, McpProfile profile, string? missing)
    {
        options.Profile = profile;
        options.Resource = missing == nameof(McpOptions.Resource) ? null : "https://resource.example/mcp";
        options.PublicOrigin = missing == nameof(McpOptions.PublicOrigin) ? null : "https://resource.example";
        if (missing != nameof(McpOptions.AuthorizationServers))
        {
            options.AuthorizationServers.Add("https://issuer.example");
        }
    }

    void Reset()
    {
        _services.Clear();
        _arc = new ArcOptions();
        _arc.EndpointGroups.Assign("capabilities", 42001);
    }

    [Fact] void should_refuse_each_missing_option_for_each_profile() => _errors.TrueForAll(_ => _ is McpOptionsInvalid).ShouldBeTrue();
    [Fact] void should_have_checked_every_combination() => _errors.Count.ShouldEqual(6);
    [Fact] void should_accept_complete_options() => _complete.ShouldBeNull();
    [Fact] void should_not_require_oauth_options_for_a_pat_only_profile() => _patOnlyWithoutOptions.ShouldBeNull();
}
