// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>Represents a read resolved by the command's event source key.</summary>
/// <param name="Name">The read model name.</param>
/// <param name="Alias">The instance alias.</param>
/// <param name="Key">The command property supplying the instance key.</param>
public record CommandReadModel(string Name, string Alias, string Key)
{
    /// <summary>Gets the source namespace owning the read model.</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>Gets the read model's declared properties.</summary>
    public IReadOnlyList<PropertyModel> Properties { get; init; } = [];
}
