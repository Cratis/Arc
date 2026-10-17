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

    /// <summary>Gets the full name of the read model type, when it was read from code.</summary>
    public string? FullName { get; init; }

    /// <summary>Gets the read model's declared properties.</summary>
    public IReadOnlyList<PropertyModel> Properties { get; init; } = [];

    /// <summary>
    /// Gets whether Arc protects this read, so it is stated as a protected command read rather than as authoring intent.
    /// </summary>
    /// <remarks>
    /// True only when the effective read protection of the command is on and neither the parameter nor the read model is
    /// declared advisory. A protected read is rejected by the model when its instance is absent, so the reader sets this
    /// only for a dependency whose absence the source also rejects.
    /// </remarks>
    public bool Protected { get; init; }
}
