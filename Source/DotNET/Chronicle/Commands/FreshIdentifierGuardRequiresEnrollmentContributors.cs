// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a command with guarded generated values cannot protect direct unit-of-work writes.
/// </summary>
/// <param name="innerException">The unsupported enrollment contributor failure.</param>
public class FreshIdentifierGuardRequiresEnrollmentContributors(Exception innerException)
    : Exception("Commands with guarded [GeneratedValue] parameters require a unit of work that supports enrollment contributors (Chronicle 19.65.2 or later).", innerException);
