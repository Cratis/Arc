// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents a failure to decide whether a part of the application is available.
/// </summary>
/// <remarks>
/// It is never a denial. Command and query results built from it are retryable errors (HTTP 503), because the state the
/// decision needs, such as a store or a context value provider, could not be read. The message is fixed text that is safe
/// to send to clients; the cause is the inner exception and is only logged.
/// </remarks>
/// <param name="innerException">The failure that stopped the decision.</param>
public class AvailabilityNotDecidedException(Exception? innerException = null)
    : Exception("Availability could not be determined. Retry the request.", innerException);
