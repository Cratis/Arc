// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Validates principal shape and its signed subject binding without projecting claims.</summary>
internal static partial class ForwardedIdentityV2Principal
{
    /// <summary>Checks the principal after signature verification.</summary>
    /// <param name="headers">The signed, canonical covered fields.</param>
    internal static void Validate(IReadOnlyDictionary<string, string> headers)
    {
        try
        {
            var text = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(headers["x-ms-client-principal"]));
            using var document = JsonDocument.Parse(text);
            var subject = ReadSubject(document.RootElement);
            var id = DecodeSubject(headers["x-ms-client-principal-id"]);
            if (id != subject) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
            var kind = headers.GetValueOrDefault("x-cratis-credential-kind");
            var service = kind == "machine-principal" || kind == "workload";
            if (!service && !SubjectPattern().IsMatch(id)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
            if (!service && headers.TryGetValue("x-cratis-subject", out var explicitSubject) && explicitSubject.Length > 0 && explicitSubject != id) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
        }
        catch (Exception error) when (error is FormatException or JsonException or DecoderFallbackException or InvalidOperationException)
        {
            throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
        }
    }

    static string ReadSubject(JsonElement root)
    {
        if (!root.TryGetProperty("userId", out var subject) || subject.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(subject.GetString())) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
        if (!root.TryGetProperty("userRoles", out var roles) || roles.ValueKind != JsonValueKind.Array || roles.EnumerateArray().Any(role => role.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(role.GetString()))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
        if (!root.TryGetProperty("claims", out var claims) || claims.ValueKind != JsonValueKind.Array || claims.EnumerateArray().Any(claim => !ValidClaim(claim))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
        return subject.GetString()!;
    }

    static bool ValidClaim(JsonElement claim) => claim.ValueKind == JsonValueKind.Object && claim.TryGetProperty("typ", out var type) && type.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(type.GetString()) && claim.TryGetProperty("val", out var value) && value.ValueKind == JsonValueKind.String;

    static string DecodeSubject(string id)
    {
        if (!id.StartsWith("UTF-8''", StringComparison.OrdinalIgnoreCase)) return id;
        var bytes = new List<byte>();
        for (var i = 7; i < id.Length; i++)
        {
            if (id[i] != '%')
            {
                bytes.Add((byte)id[i]);
                continue;
            }
            if (i + 2 >= id.Length || !Uri.IsHexDigit(id[i + 1]) || !Uri.IsHexDigit(id[i + 2])) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Identity);
            bytes.Add(Convert.ToByte(id.Substring(i + 1, 2), 16));
            i += 2;
        }
        return new UTF8Encoding(false, true).GetString(bytes.ToArray());
    }

    [GeneratedRegex("\\A[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\\z", RegexOptions.None, 100)]
    private static partial Regex SubjectPattern();
}
