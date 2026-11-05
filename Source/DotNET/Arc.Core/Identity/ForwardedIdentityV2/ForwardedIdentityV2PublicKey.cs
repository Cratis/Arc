// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Immutable public-only P-256 verification material.</summary>
internal sealed class ForwardedIdentityV2PublicKey
{
    readonly ECParameters _parameters;

    ForwardedIdentityV2PublicKey(ECParameters parameters) => _parameters = parameters;

    /// <summary>Validates a key identifier and public-only P-256 PEM.</summary>
    /// <param name="kid">The key identifier.</param>
    /// <param name="pem">The public key PEM.</param>
    /// <returns>The validated verification material.</returns>
    /// <exception cref="ArgumentException">The identifier, key visibility or curve is invalid.</exception>
    internal static ForwardedIdentityV2PublicKey FromPem(string kid, string pem)
    {
        if (!ForwardedIdentityV2Envelope.KeyIdPattern().IsMatch(kid) || pem.Contains("PRIVATE KEY", StringComparison.Ordinal)) throw new ArgumentException("A forwarded identity key must be a named P-256 public key.");
        using var key = ECDsa.Create();
        key.ImportFromPem(pem);
        var parameters = key.ExportParameters(false);
        if (parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7" || parameters.D is not null) throw new ArgumentException("A forwarded identity key must be a named P-256 public key.");
        return new(parameters);
    }

    /// <summary>Verifies a fixed-width P1363 signature without low-S restrictions.</summary>
    /// <param name="bytes">The canonical signing input.</param>
    /// <param name="signature">The fixed-width signature.</param>
    /// <returns>Whether the signature verifies.</returns>
    internal bool Verify(byte[] bytes, byte[] signature)
    {
        using var key = ECDsa.Create(_parameters);
        return key.VerifyData(bytes, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
}
