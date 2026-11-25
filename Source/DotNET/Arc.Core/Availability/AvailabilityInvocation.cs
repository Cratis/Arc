// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Describes one bound invocation whose availability is being decided.
/// </summary>
/// <param name="Part">The operation identity already selected by the pipeline.</param>
/// <param name="Arguments">The command instance or bound query arguments; null when no instance is available.</param>
/// <param name="ReceivedAt">The server's pinned UTC operation receipt, not a timestamp supplied by the caller.</param>
/// <remarks>
/// The instance is an invocation identity as well as input. Reuse it for readers of that invocation only, never for
/// another execution, even when the arguments or receipt are equal. Arguments are untrusted input, not grants or identity.
/// </remarks>
public sealed record AvailabilityInvocation(Type Part, object? Arguments, DateTimeOffset ReceivedAt);
