// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2Canonicalization;

public class when_canonicalizing : Specification
{
    readonly List<string> _failures = [];
    byte[] _original;
    byte[] _afterMutation;
    byte[] _underOtherCulture;
    byte[] _reordered;

    void Because()
    {
        var vector = ForwardedIdentityV2Vectors.Named("valid-v2");
        var settings = VerificationFixture.Settings(vector.Inputs);
        var parts = vector.Envelope.Split('.');
        var headers = VerificationFixture.Request(vector).Headers.ToArray();
        var snapshot = new ForwardedIdentityV2Request("GET", "/path%2fpart?ignored=%zz", headers, true);
        _original = ForwardedIdentityV2Canonicalization.Create(settings, snapshot, parts).Bytes;
        headers[0] = new(headers[0].Name, "changed");
        _afterMutation = ForwardedIdentityV2Canonicalization.Create(settings, snapshot, parts).Bytes;
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new("tr-TR");
            _underOtherCulture = ForwardedIdentityV2Canonicalization.Create(settings, snapshot, parts).Bytes;
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
        _reordered = ForwardedIdentityV2Canonicalization.Create(settings, new("GET", "/path%2Fpart", snapshot.Headers.Reverse().Select(header => new ForwardedIdentityV2Header(header.Name.ToUpperInvariant(), header.Value)).ToArray(), true), parts).Bytes;
        foreach (var path in new[] { "https://example.test/a", "*", "", "/a%", "/a%zz", "/a#b", "/a b", "/a\\r\n", "/é" }) Refuse(settings, new("GET", path, snapshot.Headers, true), parts, ForwardedIdentityV2Rejection.Request);
        foreach (var method in new[] { "get", "", "GÉT", "GE T" }) Refuse(settings, new(method, "/", snapshot.Headers, true), parts, ForwardedIdentityV2Rejection.Request);
        var badHeaderSets = new[]
        {
            snapshot.Headers.Append(new ForwardedIdentityV2Header("X-MS-Client-Principal", "duplicate")).ToArray(),
            snapshot.Headers.Append(new ForwardedIdentityV2Header("x-ms-client-principal-bad:name", "value")).ToArray(),
            snapshot.Headers.Append(new ForwardedIdentityV2Header("x-cratis-on-behalf-extra", "embedded\tvalue")).ToArray(),
            snapshot.Headers.Append(new ForwardedIdentityV2Header("x-cratis-on-behalf-extra", "line\r\nvalue")).ToArray()
        };
        foreach (var bad in badHeaderSets) Refuse(settings, new("GET", "/", bad, true), parts, ForwardedIdentityV2Rejection.Headers);
        Refuse(settings, new("GET", "/", snapshot.Headers.Concat(Enumerable.Range(0, 65).Select(i => new ForwardedIdentityV2Header("x-cratis-on-behalf-" + i, "v"))).ToArray(), true), parts, ForwardedIdentityV2Rejection.Bounds);
        Refuse(settings, new("GET", "/", snapshot.Headers.Append(new ForwardedIdentityV2Header("x-cratis-on-behalf-large", new string('a', 32768))).ToArray(), true), parts, ForwardedIdentityV2Rejection.Bounds);
    }

    void Refuse(ForwardedIdentityV2Settings settings, ForwardedIdentityV2Request request, string[] parts, ForwardedIdentityV2Rejection expected)
    {
        try
        {
            ForwardedIdentityV2Canonicalization.Create(settings, request, parts);
            _failures.Add("accepted malformed request");
        }
        catch (Exception error) when (ForwardedIdentityV2Rejected.Is(error, out var reason))
        {
            if (reason != expected) _failures.Add($"expected {expected}, got {reason}");
        }
    }

    [Fact] void should_preserve_a_copied_snapshot() => _afterMutation.ShouldEqual(_original);
    [Fact] void should_be_culture_invariant() => _underOtherCulture.ShouldEqual(_original);
    [Fact] void should_ignore_query_normalize_escape_hex_and_sort_case_insensitively() => _reordered.ShouldEqual(_original);
    [Fact] void should_refuse_hostile_paths_methods_and_covered_fields() => _failures.ShouldBeEmpty();
}
