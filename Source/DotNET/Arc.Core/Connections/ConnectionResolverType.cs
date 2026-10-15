// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the built-in kinds of connection resolvers.
/// </summary>
public enum ConnectionResolverType
{
    /// <summary>
    /// Resolves nothing — the configured connection is used as it is.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Resolves from templates with placeholders, see <see cref="TemplateConnectionResolver"/>.
    /// </summary>
    Template = 1,

    /// <summary>
    /// Resolves by calling an HTTP endpoint, see <see cref="HttpConnectionResolver"/>.
    /// </summary>
    Http = 2,

    /// <summary>
    /// Resolves through an <see cref="IConnectionResolver"/> implementation registered in code.
    /// </summary>
    Custom = 3
}
