// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Closed caller-resolution failures, with no credential contents.
/// </summary>
public enum CallerRefusal
{
    /// <summary>No credential was presented.</summary>
    NoCredential = 0,

    /// <summary>More than one credential or source claims the request.</summary>
    MultipleCredentials = 1,

    /// <summary>A credential was supplied in a URI.</summary>
    CredentialInUri = 2,

    /// <summary>The host cannot prove credential header cardinality.</summary>
    UnreadableCredentialHeaders = 3,

    /// <summary>No verifier claims this presentation.</summary>
    NoMatchingSource = 4,

    /// <summary>A verifier refused or was unavailable.</summary>
    SourceRefused = 5,

    /// <summary>The credential profile version is unsupported.</summary>
    UnsupportedProfile = 6,

    /// <summary>A required verified field is absent or invalid.</summary>
    MissingRequiredField = 7,

    /// <summary>The account is not an issuer plus canonical lowercase UUID subject.</summary>
    InvalidAccountKey = 8,

    /// <summary>A service tried to carry person or delegation fields.</summary>
    ServiceCarriesPersonFields = 9,

    /// <summary>A delegated actor has no verified grant.</summary>
    GrantMissing = 10,

    /// <summary>A delegation binding differs from the credential.</summary>
    GrantMismatch = 11,

    /// <summary>A sender-bound credential has no proof.</summary>
    SenderBindingRequired = 12,

    /// <summary>The verified proof does not match the credential.</summary>
    SenderBindingMismatch = 13,

    /// <summary>The proof was replayed.</summary>
    SenderBindingReplayed = 14,

    /// <summary>The receiving installation has no registered resource and environment.</summary>
    InstallationNotConfigured = 15,

    /// <summary>The credential targets another resource.</summary>
    ResourceMismatch = 16,

    /// <summary>The credential audience is not the catalog audience.</summary>
    AudienceMismatch = 17,

    /// <summary>The credential targets another environment.</summary>
    EnvironmentMismatch = 18,

    /// <summary>The verified and resolved tenants differ.</summary>
    TenantMismatch = 19,

    /// <summary>The Authorization value is not exactly one RFC 6750 bearer.</summary>
    MalformedAuthorization = 20,

    /// <summary>More than one source is registered for the selected shape.</summary>
    SourceConflict = 21,

    /// <summary>The request inputs consumed by an earlier resolution changed.</summary>
    RequestChanged = 22,

    /// <summary>Another scheme authenticated a different subject on the request.</summary>
    AmbiguousPrincipal = 23
}
