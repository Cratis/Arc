// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts;

/// <summary>
/// Defines the source of the request-scoped context values a model provides.
/// </summary>
/// <remarks>
/// Register it as a scoped service, so every value resolves lazily, once, for the request that owns the scope. A host that
/// registers none pays nothing: <see cref="ContextValues"/> then reads every value as absent. <c>AddArcAvailability</c> registers one
/// built on the Choreography engine.
/// </remarks>
public interface IContextValueSource
{
    /// <summary>
    /// Reads a context value, resolving it on first read and reusing the result for the rest of the request.
    /// </summary>
    /// <param name="part">The part (command, query performer or other artifact) the value is read for, which decides the level whose declaration wins; <see langword="null"/> for the application level.</param>
    /// <param name="name">The name of the value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> of the request.</param>
    /// <returns>The <see cref="ContextValueRead"/>.</returns>
    /// <exception cref="RequiredContextValueAbsentException">A value declared as required is absent.</exception>
    /// <exception cref="ContextValueUnavailableException">The value could not be resolved. The request fails as a retryable error.</exception>
    ValueTask<ContextValueRead> Read(Type? part, string name, CancellationToken cancellationToken);
}
