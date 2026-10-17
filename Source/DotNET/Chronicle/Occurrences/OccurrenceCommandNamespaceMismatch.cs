// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when the event store namespace a tenant maps to differs from the namespace of the occurrence.
/// </summary>
/// <param name="expected">The namespace of the occurrence.</param>
/// <param name="actual">The namespace the tenant resolved to.</param>
public class OccurrenceCommandNamespaceMismatch(string expected, string actual)
    : Exception($"The occurrence is delivered in namespace '{expected}' but the tenant mapping resolved namespace '{actual}'. The command was not executed.");
