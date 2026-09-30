// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators;

/// <summary>
/// Represents the generated registration of the members to walk for one model type.
/// </summary>
internal sealed record ModelGraphWalker
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelGraphWalker"/> class.
    /// </summary>
    /// <param name="type">The fully qualified name of the type the members are walked for.</param>
    /// <param name="registration">The statement registering the members.</param>
    public ModelGraphWalker(string type, string registration)
    {
        Type = type;
        Registration = registration;
    }

    /// <summary>
    /// Gets the fully qualified name of the type the members are walked for.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the statement registering the members.
    /// </summary>
    public string Registration { get; }
}
