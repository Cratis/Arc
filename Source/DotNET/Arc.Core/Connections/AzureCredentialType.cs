// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Defines which Azure credential acquires the token when authenticating with <see cref="HttpConnectionResolverAuthenticationType.AzureCredential"/>.
/// </summary>
public enum AzureCredentialType
{
    /// <summary>
    /// A managed identity: system-assigned, or user-assigned when a client id is set.
    /// </summary>
    ManagedIdentity = 0,

    /// <summary>
    /// A workload identity, for instance on Kubernetes, configured from the environment.
    /// </summary>
    WorkloadIdentity = 1,

    /// <summary>
    /// The default Azure credential chain, which also covers developer sign-ins for local development.
    /// </summary>
    Default = 2
}
