// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

/// <summary>
/// Runs the type-mapping specs one at a time.
/// </summary>
/// <remarks>
/// The mapping table is a static that <see cref="TypeExtensions.SetTypeMappings"/> replaces wholesale, and xUnit
/// runs spec classes in parallel - so two classes mapping the same type read each other's table and fail on which
/// one won. Clearing the table afterwards, which <c>given.no_type_mappings</c> does, is necessary but cannot fix
/// that: the race is between one class's setup and another's assertion, both inside their own lifetimes.
/// <para>
/// Serializing them is the honest fix rather than giving each spec its own type to map. The table really is
/// process-wide, and a spec suite that only passed because no two specs happened to name the same type would go on
/// passing right up until someone reused one.
/// </para>
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class TypeMappingCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "TypeMapping";
}
