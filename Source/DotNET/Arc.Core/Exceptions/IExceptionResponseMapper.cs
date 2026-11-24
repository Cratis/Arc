// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Exceptions;

/// <summary>
/// Defines convention-discovered, application-owned mappings from exceptions to safe failure responses.
/// </summary>
/// <remarks>
/// Implementations are resolved from the request or subscription scope through Cratis.Types.IInstancesOf{T}.
/// The first accepting mapper wins. Codes and headers must contain only intentionally public information,
/// never exception messages, stack traces or request-derived values.
/// </remarks>
public interface IExceptionResponseMapper
{
    /// <summary>
    /// Tries to select a public failure response for an exception.
    /// </summary>
    /// <param name="exception">The original failure, for typed matching.</param>
    /// <param name="response">The safe response, or null when declined.</param>
    /// <returns>Whether the exception was accepted.</returns>
    bool TryMap(Exception exception, [NotNullWhen(true)] out ExceptionResponse? response);
}
