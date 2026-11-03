// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>Describes a provider-backed lookup declaration without reflective provider activation.</summary>
internal interface ILookUpByKeyProvider
{
    /// <summary>Gets the provider type.</summary>
    Type ProviderType { get; }

    /// <summary>Gets the shared stateless provider.</summary>
    IReadModelKeyProvider Provider { get; }
}
