// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when a discovered open generic read model interceptor does not implement
/// <see cref="IInterceptReadModel{TReadModel}"/> over its own single type parameter.
/// </summary>
/// <remarks>
/// Such an interceptor can never be closed for a read model, so it would silently skip interception. Because
/// interceptors are used for concerns such as redaction, that would fail open. It is rejected when the interceptors are discovered instead.
/// </remarks>
/// <param name="interceptorType">The open generic interceptor <see cref="Type"/>.</param>
public class OpenGenericReadModelInterceptorMustInterceptItsTypeParameter(Type interceptorType)
    : Exception($"The open generic read model interceptor '{interceptorType}' is not supported. Only 'IInterceptReadModel<T>' over the interceptor's own single type parameter 'T' is supported for open generic interceptors.");
