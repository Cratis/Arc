// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Marks a Handle parameter as a server-generated value rather than a request input or dependency.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class GeneratedValueAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedValueAttribute"/> class using the parameter name.
    /// </summary>
    public GeneratedValueAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedValueAttribute"/> class.
    /// </summary>
    /// <param name="name">The name used to identify the generated value.</param>
    public GeneratedValueAttribute(string name) => Name = name;

    /// <summary>
    /// Gets the explicit name, or null to use the parameter name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets or sets the freshness policy when the generated value is used as an append target.
    /// </summary>
    public GeneratedValueFreshness Freshness { get; set; } = GeneratedValueFreshness.Guarded;
}
