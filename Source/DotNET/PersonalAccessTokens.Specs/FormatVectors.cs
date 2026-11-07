// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Exercises only locked construction data, not credential verification or policy.</summary>
internal static class FormatVectors
{
    /// <summary>Adds an independently computed checksum to a synthetic body.</summary>
    /// <param name="body">The synthetic body.</param>
    /// <returns>The checksummed synthetic value.</returns>
    internal static string WithChecksum(string body)
    {
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(body))).ToLowerInvariant()[..8];
        return $"{body}_{checksum}";
    }

    /// <summary>Reads a synthetic token from the locked construction vectors.</summary>
    /// <param name="resourceKey">The vector resource key.</param>
    /// <returns>The synthetic token.</returns>
    internal static string Token(string resourceKey)
    {
        using var stream = typeof(FormatVectors).Assembly.GetManifestResourceStream("Cratis.Arc.PersonalAccessTokens.Vectors.format-v1.json")!;
        using var document = JsonDocument.Parse(stream);
        var construction = document.RootElement.GetProperty("construction");
        var secret = construction.GetProperty("secretHex").GetString();
        var checksum = construction.GetProperty("expectedChecksums").GetProperty(resourceKey).GetString();
        return $"{resourceKey}_pat_v1_{secret}_{checksum}";
    }
}
