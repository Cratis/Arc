// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_superseded_files;

/// <summary>
/// Simple descriptor implementation for testing purposes.
/// </summary>
/// <param name="Type">The type the descriptor represents.</param>
/// <param name="Name">The name of the descriptor.</param>
public record TestDescriptor(Type Type, string Name) : IDescriptor
{
    /// <summary>
    /// Gets the types involved — empty for tests.
    /// </summary>
    public IEnumerable<Type> TypesInvolved => [];
}
