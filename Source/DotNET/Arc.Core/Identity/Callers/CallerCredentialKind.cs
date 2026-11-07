// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// The verified credential category.
/// </summary>
public enum CallerCredentialKind
{
    /// <summary>A persisted personal access token.</summary>
    PersonalAccessToken = 0,

    /// <summary>An OAuth credential with any required grant mapping.</summary>
    OAuthGrant = 1,

    /// <summary>An authenticated machine credential.</summary>
    MachinePrincipal = 2,

    /// <summary>A verified browser session with credential identity and assurance.</summary>
    BrowserSession = 3,

    /// <summary>A checked on-behalf session.</summary>
    OnBehalfSession = 4,

    /// <summary>A separately verified workload credential.</summary>
    Workload = 5
}
