// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when the count and paging operations for a queryable's element type cannot be created
/// because no registration was generated for it and the code is ahead-of-time compiled, as under NativeAOT.
/// </summary>
/// <remarks>
/// Thrown only when the element type has no generated registration and creating its operations through reflection is
/// not supported, which happens in ahead-of-time compiled code such as NativeAOT. Under the JIT such element types are
/// handled through a cached reflection path. The Arc source generator registers the model-bound read models that have a
/// query returning a collection of them. Declare the concrete collection type on the query method of a model-bound read
/// model rather than a base type or object.
/// </remarks>
/// <param name="elementType">The element type of the queryable.</param>
public class MissingQueryableElementType(Type elementType)
    : Exception($"No generated queryable registration exists for element type '{elementType}', and its operations cannot be created at runtime. Declare the concrete collection return type, such as IQueryable<T>, on the query method of a model-bound read model.");
