// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when a queryable cannot be sorted on a property because no registration was generated for
/// the property type and the code is ahead-of-time compiled, as under NativeAOT.
/// </summary>
/// <remarks>
/// Thrown only when the element type and property type have no generated registration and sorting through reflection is
/// not supported, which happens in ahead-of-time compiled code such as NativeAOT. Under the JIT such sorts are handled
/// through reflection. The Arc source generator registers the type of each public property of the model-bound read
/// models that have a query returning a collection of them.
/// </remarks>
/// <param name="elementType">The element type of the queryable.</param>
/// <param name="keyType">The type of the property to sort on.</param>
public class MissingQueryableSortKey(Type elementType, Type keyType)
    : Exception($"No generated queryable sort registration exists for element type '{elementType}' and property type '{keyType}', and sorting cannot be set up at runtime. Declare the concrete collection return type, such as IQueryable<T>, on the query method of a model-bound read model.");
