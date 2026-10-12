// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Attribute to mark a class as a read model.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ReadModelAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether only methods marked with <see cref="QueryAttribute"/> are exposed as queries.
    /// </summary>
    /// <remarks>The default is false, preserving conventional static-method discovery.</remarks>
    public bool RequireQueryAttribute { get; set; }
}
