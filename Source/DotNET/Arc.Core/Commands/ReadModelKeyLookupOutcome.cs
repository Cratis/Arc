// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The outcome of resolving a caller-derived read-model key.
/// </summary>
public enum ReadModelKeyLookupOutcome
{
    /// <summary>No key is available.</summary>
    Missing = 0,

    /// <summary>One key is available.</summary>
    Found = 1,

    /// <summary>More than one distinct key is available.</summary>
    Ambiguous = 2
}
