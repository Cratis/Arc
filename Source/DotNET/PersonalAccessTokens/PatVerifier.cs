// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Domain-separated one-way verifier derivation and constant-time digest comparison.</summary>
internal static class PatVerifier
{
    /// <summary>Derives a storage verifier without retaining input bytes.</summary>
    /// <param name="value">The canonical bearer value. Never log it.</param>
    /// <returns>A one-way verifier.</returns>
    internal static string Derive(string value)
    {
        var bytes = Encoding.ASCII.GetBytes("cratis-arc-pat-verifier-v1\0" + value);
        try
        {
            return Convert.ToHexString(SHA256.HashData(bytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    /// <summary>Checks equal-length digest bytes in constant time.</summary>
    /// <param name="expected">The derived digest.</param>
    /// <param name="actual">The authoritative stored digest.</param>
    /// <returns>Whether the digests match.</returns>
    internal static bool Matches(string expected, string actual)
    {
        if (expected.Length != 64 || actual.Length != 64) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(actual));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
