// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Exceptions;

/// <summary>
/// Retains one failure for the lifetime of its result, never as part of the wire contract.
/// </summary>
/// <param name="exception">The original exception.</param>
internal sealed class ExceptionResponseCandidate(Exception exception)
{
    /// <summary>Gets the original exception for typed mapping.</summary>
    internal Exception Exception { get; } = exception;

    /// <summary>Gets or sets whether mapping has already been attempted.</summary>
    internal bool IsAttempted { get; set; }

    /// <summary>Gets or sets the accepted, validated response.</summary>
    internal ExceptionResponse? Response { get; set; }
}
