// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

#pragma warning disable SA1600 // Elements should be documented - each value is its own documentation

/// <summary>
/// The reasons an on-behalf forward fails authentication.
/// </summary>
internal static class OnBehalfOfFailures
{
    internal const string UntrustedForward = "Not authenticated - on-behalf forwarding requires trusted forwarded identity headers";
    internal const string NotEnabled = "Not authenticated - on-behalf forwarding is not enabled on this host";
    internal const string Misconfigured = "Not authenticated - on-behalf forwarding is misconfigured";
    internal const string MissingRepresentedUser = "Not authenticated - on-behalf forward does not identify the represented user";
    internal const string RepeatedHeader = "Not authenticated - an on-behalf header was repeated";
    internal const string MissingActor = "Not authenticated - on-behalf forward has an empty actor";
    internal const string InvalidActor = "Not authenticated - invalid representation of the on-behalf actor";
    internal const string ActorIsRepresentedUser = "Not authenticated - an identity cannot act on behalf of itself";
    internal const string MissingAssertion = "Not authenticated - on-behalf forward carries no assertion";
    internal const string MalformedAssertion = "Not authenticated - the on-behalf assertion is malformed";
    internal const string UnsupportedAlgorithm = "Not authenticated - the on-behalf assertion is not signed with RS256";
    internal const string UnknownSigningKey = "Not authenticated - the on-behalf assertion is signed with an unknown key";
    internal const string InvalidSignature = "Not authenticated - the on-behalf assertion signature is invalid";
    internal const string InvalidIssuer = "Not authenticated - the on-behalf assertion has the wrong issuer";
    internal const string InvalidAudience = "Not authenticated - the on-behalf assertion has the wrong audience";
    internal const string MissingLifetime = "Not authenticated - the on-behalf assertion has no nbf or exp";
    internal const string NotYetValid = "Not authenticated - the on-behalf assertion is not yet valid";
    internal const string Expired = "Not authenticated - the on-behalf assertion has expired";
    internal const string LifetimeTooLong = "Not authenticated - the on-behalf assertion lives longer than five minutes";
    internal const string ServiceMismatch = "Not authenticated - the on-behalf assertion is for another service";
    internal const string WrongPurpose = "Not authenticated - the on-behalf assertion is not for a forwarded request";
    internal const string MissingTokenId = "Not authenticated - the on-behalf assertion has no jti";
    internal const string PrincipalMismatch = "Not authenticated - the on-behalf assertion does not match the represented user's headers";
    internal const string ActorMismatch = "Not authenticated - the on-behalf assertion does not match the actor header";
    internal const string TenantMismatch = "Not authenticated - the on-behalf assertion is for another tenant";
    internal const string MethodMismatch = "Not authenticated - the on-behalf assertion is for another HTTP method";
    internal const string TargetMismatch = "Not authenticated - the on-behalf assertion is for another target";
    internal const string Replayed = "Not authenticated - the on-behalf assertion has already been used";
}
