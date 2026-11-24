// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// Defines the request-scoped view of '$context': the values a request carries and the ones models provide.
/// </summary>
/// <remarks>
/// A provided value is resolved lazily, when it is first read, and the result is reused for the rest of the request, so
/// every gate in the request sees one consistent value. There is no cross-request cache.
/// </remarks>
public interface IContextValueResolver
{
    /// <summary>
    /// Reads a value by path, for example 'vessel.commercialPackage' or '$context.audiences'.
    /// </summary>
    /// <param name="path">The dotted path. The first segment names the value; an optional '$context.' prefix is ignored.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The outcome; absent when any part of the path is missing.</returns>
    /// <exception cref="RequiredContextValueAbsentException">A required value is absent.</exception>
    /// <exception cref="ContextValueCycleException">Values read each other in a cycle.</exception>
    /// <exception cref="ContextValueProviderFailedException">A provider failed.</exception>
    ValueTask<ContextValueOutcome> Resolve(string path, CancellationToken cancellationToken = default);
}
