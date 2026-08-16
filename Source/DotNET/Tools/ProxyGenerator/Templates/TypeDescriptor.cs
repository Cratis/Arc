// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Describes a type for templating purposes.
/// </summary>
/// <param name="Type">Original type.</param>
/// <param name="Name">Name of the type.</param>
/// <param name="Properties">Properties on the type.</param>
/// <param name="Imports">Additional import statements.</param>
/// <param name="TypesInvolved">Collection of types involved in the command.</param>
/// <param name="Documentation">JSDoc documentation for the type.</param>
/// <param name="DerivedTypeId">The derived type identifier GUID string if this type carries a <c>DerivedTypeAttribute</c>, otherwise null.</param>
/// <param name="BaseTypeName">The TypeScript name of the base type to extend, or null if there is no applicable base type.</param>
public record TypeDescriptor(
    Type Type,
    string Name,
    IEnumerable<PropertyDescriptor> Properties,
    IOrderedEnumerable<ImportStatement> Imports,
    IEnumerable<Type> TypesInvolved,
    string? Documentation = null,
    string? DerivedTypeId = null,
    string? BaseTypeName = null) : IDescriptor;
