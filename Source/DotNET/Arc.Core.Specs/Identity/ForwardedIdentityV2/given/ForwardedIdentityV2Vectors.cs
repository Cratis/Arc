// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal static class ForwardedIdentityV2Vectors
{
    static readonly Lazy<Fixture> _fixture = new(Load);
    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    internal static Fixture Fixture => _fixture.Value;
    internal static Vector Named(string name) => Fixture.Cases.Single(item => item.Name == name);

    static Fixture Load()
    {
        using var stream = typeof(ForwardedIdentityV2Vectors).Assembly.GetManifestResourceStream("ForwardedIdentityV2Vectors.json")!;
        var fixture = JsonSerializer.Deserialize<Fixture>(stream, _json)!;
        if (fixture.Format != "Cratis.AuthProxy.ForwardedIdentity.v2" || fixture.Cases.Length < 20 || fixture.Cases.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != fixture.Cases.Length)
        {
            throw new InvalidOperationException("Invalid forwarded identity vector fixture.");
        }
        return fixture;
    }
}
