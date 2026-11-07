// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>
/// Framework-created owner binding required by atomic revocation.
/// Adapters may inspect this value but cannot create or change it.
/// </summary>
public sealed record PatOwner
{
    /// <summary>Initializes a new instance of the <see cref="PatOwner"/> class.</summary>
    /// <param name="resource">The checked resource.</param>
    /// <param name="environment">The checked environment.</param>
    /// <param name="person">The verified person.</param>
    /// <param name="accountKey">The verified account.</param>
    /// <param name="tenant">The verified tenant.</param>
    internal PatOwner(string resource, string environment, CallerPersonId person, CallerAccountKey accountKey, TenantId tenant)
    {
        Resource = resource;
        Environment = environment;
        Person = person;
        AccountKey = accountKey;
        Tenant = tenant;
    }

    /// <summary>
    /// Gets the receiving resource.
    /// </summary>
    public string Resource { get; internal init; }

    /// <summary>
    /// Gets the receiving environment.
    /// </summary>
    public string Environment { get; internal init; }

    /// <summary>
    /// Gets the verified person.
    /// </summary>
    public CallerPersonId Person { get; internal init; }

    /// <summary>
    /// Gets the verified upstream account.
    /// </summary>
    public CallerAccountKey AccountKey { get; internal init; }

    /// <summary>
    /// Gets the verified tenant.
    /// </summary>
    public TenantId Tenant { get; internal init; }
}
