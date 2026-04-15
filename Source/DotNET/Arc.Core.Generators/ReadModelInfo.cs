// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators;

/// <summary>
/// Holds information about a read model type for use during source generation.
/// </summary>
internal sealed class ReadModelInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelInfo"/> class.
    /// </summary>
    /// <param name="fullyQualifiedTypeName">The fully qualified type name of the read model.</param>
    /// <param name="queryMethodNames">The names of the valid query methods on the read model.</param>
    internal ReadModelInfo(string fullyQualifiedTypeName, List<string> queryMethodNames)
    {
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        QueryMethodNames = queryMethodNames;
    }

    /// <summary>
    /// Gets the fully qualified type name of the read model.
    /// </summary>
    internal string FullyQualifiedTypeName { get; }

    /// <summary>
    /// Gets the names of the valid query methods on the read model.
    /// </summary>
    internal List<string> QueryMethodNames { get; }
}
