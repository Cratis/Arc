// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Capabilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp.for_McpVectors.given;

/// <summary>
/// Classifies every pinned vector and checks the ones that describe the resource server against Arc's MCP surface.
/// </summary>
public static class McpVectorRunner
{
    /// <summary>
    /// The number of vectors the pinned file holds.
    /// </summary>
    public const int VectorCount = 48;

    const string Verifier = "Credential verification and verifier selection belong to the identity verifiers, not the MCP endpoint.";
    const string Client = "Describes MCP client behavior.";
    const string AuthorizationServer = "Describes authorization server behavior.";

    static readonly int[] _unauthorized = [0, 4, 10, 27];
    static readonly int[] _metadata = [1, 5, 11];
    static readonly int[] _insufficientScope = [18, 19];
    static readonly int[] _forbidden = [25, 26];
    static readonly int[] _invalidRequest = [40, 41, 46, 47];

    static readonly Dictionary<int, string> _notApplicable = new()
    {
        [2] = Verifier,
        [3] = Verifier,
        [6] = Verifier,
        [7] = Verifier,
        [8] = Verifier,
        [9] = Verifier,
        [12] = Verifier,
        [13] = Client,
        [14] = Client,
        [15] = AuthorizationServer,
        [16] = AuthorizationServer,
        [17] = AuthorizationServer,
        [20] = Client,
        [21] = Client,
        [22] = Client,
        [23] = Client,
        [24] = Verifier,
        [28] = Verifier,
        [29] = Verifier,
        [30] = Verifier,
        [31] = Verifier,
        [32] = Verifier,
        [33] = Verifier,
        [34] = Verifier,
        [35] = Verifier,
        [36] = Client,
        [37] = AuthorizationServer,
        [38] = AuthorizationServer,
        [39] = AuthorizationServer,
        [42] = Verifier,
        [43] = Verifier,
        [44] = Verifier,
        [45] = Verifier
    };

    /// <summary>
    /// Gets the indexes of the vectors that describe the resource server.
    /// </summary>
    public static IEnumerable<int> Applicable =>
        _unauthorized.Concat(_metadata).Concat(_insufficientScope).Concat(_forbidden).Concat(_invalidRequest);

    /// <summary>
    /// Gets the indexes of the vectors that describe another party, with the reason.
    /// </summary>
    public static IReadOnlyDictionary<int, string> NotApplicable => _notApplicable;

    /// <summary>
    /// Checks one applicable vector.
    /// </summary>
    /// <param name="index">The vector index.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The mismatches found; empty when the vector holds.</returns>
    public static async Task<IEnumerable<string>> Check(int index, JsonNode vector)
    {
        var mismatches = new List<string>();
        foreach (var profile in Profiles(vector["given"]!))
        {
            var options = OptionsFor(profile);
            var context = await Respond(index, vector, options);
            mismatches.AddRange(Compare(vector["then"]!, context).Select(_ => $"#{index} ({profile}): {_}"));
        }

        return mismatches;
    }

    static IEnumerable<McpProfile> Profiles(JsonNode given)
    {
        var names = given["profiles"] is JsonArray profiles
            ? profiles.Select(_ => _!.GetValue<string>())
            : [given["profile"]!.GetValue<string>()];
        return names.Select(_ => _ switch
        {
            "pat-only" => McpProfile.PatOnly,
            "oauth-only" => McpProfile.OAuthOnly,
            "combined" => McpProfile.Combined,
            _ => throw new ArgumentOutOfRangeException(nameof(given), _, "Unknown profile.")
        });
    }

    static McpOptions OptionsFor(McpProfile profile)
    {
        var options = new McpOptions
        {
            Profile = profile,
            ApplicationName = "direct",
            Resource = "https://direct.example.test/mcp",
            PublicOrigin = "https://direct.example.test",
            ReadScope = "direct:read",
            WriteScope = "direct:write"
        };
        options.AuthorizationServers.Add("https://login.example.test");
        return options;
    }

    static async Task<HttpContext> Respond(int index, JsonNode vector, McpOptions options)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(Options.Create(options)).BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        var writer = new McpRefusalWriter(Options.Create(options));

        if (_metadata.Contains(index))
        {
            await McpProtectedResourceMetadata.Serve(context);
        }
        else if (_insufficientScope.Contains(index))
        {
            var scopes = vector["when"]!["requiredScopes"]!.AsArray().Select(_ => _!.GetValue<string>());
            var challenge = McpChallenge.InsufficientScope(options, scopes, stepUp: true);
            await McpChallenge.Write(context, StatusCodes.Status403Forbidden, "insufficient_scope", challenge);
        }
        else
        {
            var status = _forbidden.Contains(index) ? StatusCodes.Status403Forbidden
                : _invalidRequest.Contains(index) ? StatusCodes.Status400BadRequest
                : StatusCodes.Status401Unauthorized;
            await writer.Write(context, new CapabilityRefusal(status, null));
        }

        return context;
    }

    static IEnumerable<string> Compare(JsonNode then, HttpContext context)
    {
        var response = context.Response;
        var challenge = response.Headers.WWWAuthenticate.ToString();
        if (then["status"] is { } status && status.GetValue<int>() != response.StatusCode)
        {
            yield return $"status {response.StatusCode}, expected {status}";
        }

        if (then["contentType"] is { } contentType && contentType.GetValue<string>() != response.ContentType)
        {
            yield return $"content type {response.ContentType}, expected {contentType}";
        }

        if (then["html"]?.GetValue<bool>() == false && response.ContentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true)
        {
            yield return "answered with HTML";
        }

        if (then["locationHeaderPresent"]?.GetValue<bool>() == false && response.Headers.Location.Count > 0)
        {
            yield return "answered with a Location header";
        }

        if (then["wwwAuthenticate"] is { } expected && expected.GetValue<string>() != challenge)
        {
            yield return $"challenge '{challenge}', expected '{expected}'";
        }

        var noMetadata = then["advertisesOAuthMetadata"]?.GetValue<bool>() == false || then["oauthStepUpSuggested"]?.GetValue<bool>() == false;
        if (noMetadata && challenge.Contains("resource_metadata", StringComparison.Ordinal))
        {
            yield return "advertised OAuth metadata";
        }

        if (then["body"] is { } body)
        {
            response.Body.Position = 0;
            var actual = JsonNode.Parse(response.Body);
            if (!JsonNode.DeepEquals(body, actual))
            {
                yield return $"body {actual?.ToJsonString()}, expected {body.ToJsonString()}";
            }
        }
    }
}
