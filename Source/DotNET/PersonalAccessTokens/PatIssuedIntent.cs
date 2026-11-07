// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Secret-free issuance intent, committed atomically with its verifier record.</summary>
/// <param name="CredentialId">Non-secret identifier.</param>
/// <param name="CreatedAt">Issuance timestamp.</param>
internal sealed record PatIssuedIntent(CredentialId CredentialId, DateTimeOffset CreatedAt);
