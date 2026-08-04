// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

/// <summary>
/// Serializes the specs that configure type mappings.
/// </summary>
/// <remarks>
/// The mapping table is a static that <c>SetTypeMappings</c> replaces wholesale, so two specs
/// configuring it at the same time each see the other's table rather than their own. That is not a race
/// distinct subject types can avoid - the replacement takes the whole table with it - so the specs have
/// to be serialized instead. Parallelization is disabled for the collection rather than only within it,
/// because a mapping installed here is visible to any spec in the assembly that resolves a target type.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class TypeMappingCollectionDefinition
{
    /// <summary>
    /// The name specs join this collection under.
    /// </summary>
    public const string Name = "Type mapping configuration";
}
