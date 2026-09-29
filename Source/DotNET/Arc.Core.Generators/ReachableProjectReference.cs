// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators;

/// <summary>
/// Represents a project reference generated code reaches through a type in it.
/// </summary>
internal sealed record ReachableProjectReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReachableProjectReference"/> class.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <param name="typeName">The fully qualified name of the type generated code names.</param>
    public ReachableProjectReference(string assemblyName, string typeName)
    {
        AssemblyName = assemblyName;
        TypeName = typeName;
    }

    /// <summary>
    /// Gets the name of the assembly.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the fully qualified name of the type generated code names.
    /// </summary>
    public string TypeName { get; }
}
