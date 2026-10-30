// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when the operations for an observable query's element type cannot be created because no
/// registration was generated for it and the code is ahead-of-time compiled, as under NativeAOT.
/// </summary>
/// <remarks>
/// Thrown only when the element type has no generated registration and creating its operations through reflection is
/// not supported, which happens in ahead-of-time compiled code such as NativeAOT. Under the JIT such element types are
/// handled through a cached reflection path. The Arc source generator registers the element types of the
/// <see cref="System.Reactive.Subjects.ISubject{T}"/> and <see cref="IAsyncEnumerable{T}"/> results that model-bound
/// read model queries declare. Declare the concrete result type on the query method rather than a base type or object.
/// </remarks>
/// <param name="elementType">The element type of the observable query result.</param>
public class MissingObservableQueryElementType(Type elementType)
    : Exception($"No generated observable query registration exists for element type '{elementType}', and its operations cannot be created at runtime. Declare the concrete ISubject<T> or IAsyncEnumerable<T> return type on the query method of a model-bound read model.");
