// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_public_urls_and_origins_are_unsafe : a_startup_validation
{
    const string Origin = "https://resource.example";

    Dictionary<string, Exception?> _errors;
    Exception? _loopback;
    Exception? _trailingSlash;

    void Because()
    {
        _errors = [];
        Check("plain http resource", Origin, "http://resource.example/mcp", []);
        Check("plain http origin", "http://resource.example", "http://resource.example/mcp", []);
        Check("fragment in resource", Origin, $"{Origin}/mcp#frag", []);
        Check("empty fragment in resource", Origin, $"{Origin}/mcp#", []);
        Check("other host in resource", Origin, "https://elsewhere.example/mcp", []);
        Check("other scheme in resource", Origin, "http://resource.example/mcp", []);
        Check("other path in resource", Origin, $"{Origin}/other", []);
        Check("query in resource", Origin, $"{Origin}/mcp?x=1", []);
        Check("user info in resource", Origin, "https://user@resource.example/mcp", []);
        Check("path in origin", $"{Origin}/base", $"{Origin}/base/mcp", []);
        Check("relative resource", Origin, "/mcp", []);
        Check("plain http allowed origin", Origin, $"{Origin}/mcp", ["http://app.example"]);
        Check("allowed origin with a path", Origin, $"{Origin}/mcp", ["https://app.example/path"]);
        Check("blank allowed origin", Origin, $"{Origin}/mcp", [" "]);

        _loopback = Run("http://localhost:5000", "http://localhost:5000/mcp", ["http://127.0.0.1:3000"]);
        _trailingSlash = Run(Origin, $"{Origin}/mcp/", []);
    }

    [Fact] void should_refuse_every_unsafe_value() => _errors.Where(_ => _.Value is not McpOptionsInvalid).Select(_ => _.Key).ShouldBeEmpty();
    [Fact] void should_have_checked_every_case() => _errors.Count.ShouldEqual(14);
    [Fact] void should_accept_plain_http_for_loopback() => _loopback.ShouldBeNull();
    [Fact] void should_accept_a_trailing_slash_on_the_resource() => _trailingSlash.ShouldBeNull();

    void Check(string label, string origin, string resource, string[] allowed) => _errors[label] = Run(origin, resource, allowed);

    Exception? Run(string origin, string resource, string[] allowed)
    {
        _services.Clear();
        _arc = new ArcOptions();
        _arc.EndpointGroups.Assign("capabilities", 42001);
        _mcp = mcp => mcp.Configure(options =>
        {
            options.Profile = McpProfile.Combined;
            options.PublicOrigin = origin;
            options.Resource = resource;
            options.AuthorizationServers.Add("https://issuer.example");
            foreach (var value in allowed)
            {
                options.AllowedOrigins.Add(value);
            }
        });
        Validate();
        return _error;
    }
}
