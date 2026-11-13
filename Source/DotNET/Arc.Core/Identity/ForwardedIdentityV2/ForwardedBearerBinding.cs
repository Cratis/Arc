// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Binds an exact-token hash to independent bearer verification and account equality.</summary>
internal static partial class ForwardedBearerBinding
{
    /// <summary>Checks signed bearer coverage and dispatches exactly one verifier.</summary>
    /// <param name="request">The immutable raw request.</param>
    /// <param name="context">The verified signed context.</param>
    /// <param name="verifier">The independent credential verifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The independent bearer facts, or null when no bearer is present.</returns>
    internal static async Task<ForwardedBearerVerification?> Bind(ForwardedIdentityV2Request request, ForwardedIdentityV2Context context, IForwardedBearerVerifier verifier, CancellationToken cancellationToken)
    {
        var authorization = ReadAuthorization(request, context);
        if (authorization is null) return null;
        var token = authorization[7..];
        CheckHash(token, context.ForwardedTokenHash!);
        var kind = SelectVerifier(token, context);
        var result = await Dispatch(() => verifier.Verify(kind, authorization, context, cancellationToken));
        if (!Matches(result, kind, context)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
        return result;
    }

    static string? ReadAuthorization(ForwardedIdentityV2Request request, ForwardedIdentityV2Context context)
    {
        var values = request.Headers.Where(header => header.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (values.Length == 0)
        {
            if (context.ForwardedTokenHash is not null) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
            return null;
        }
        if (values.Length != 1 || context.ForwardedTokenHash is null || !values[0].Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
        return values[0].Value;
    }

    static ForwardedBearerVerifierKind SelectVerifier(string token, ForwardedIdentityV2Context context)
    {
        var patNamespace = PatNamespace().IsMatch(token);
        if (patNamespace && context.CredentialKind != CallerCredentialKind.PersonalAccessToken) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
        return patNamespace ? ForwardedBearerVerifierKind.Pat : ForwardedBearerVerifierKind.OAuth;
    }

    static bool Matches(ForwardedBearerVerification? result, ForwardedBearerVerifierKind kind, ForwardedIdentityV2Context context) =>
        result?.IsAccepted == true && result.Kind == kind && result.AccountKey == new CallerAccountKey(context.Issuer ?? string.Empty, context.Subject ?? string.Empty);

    static void CheckHash(string token, string hash)
    {
        var unpadded = token.TrimEnd('=');
        if (unpadded.Length == 0 || unpadded.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '.' or '_' or '~' or '+' or '/'))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
        byte[] expected;
        try
        {
            expected = ForwardedIdentityV2Envelope.DecodeUrl(hash, 32);
        }
        catch (Exception error) when (ForwardedIdentityV2Rejected.Is(error, out _))
        {
            throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
        }
        if (!CryptographicOperations.FixedTimeEquals(expected, SHA256.HashData(Encoding.UTF8.GetBytes(token)))) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
    }

    static async Task<ForwardedBearerVerification> Dispatch(Func<Task<ForwardedBearerVerification>> verify)
    {
        try
        {
            return await verify();
        }
        catch (Exception)
        {
            throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Credential);
        }
    }

    [GeneratedRegex("^[a-z0-9-]+_pat_", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    private static partial Regex PatNamespace();
}
