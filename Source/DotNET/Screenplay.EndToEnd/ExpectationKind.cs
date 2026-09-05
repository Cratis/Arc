// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Represents what an <see cref="Expectation"/> is about.
/// </summary>
public enum ExpectationKind
{
    /// <summary>
    /// A line the document has to carry, matched on the line with its indentation trimmed.
    /// </summary>
    Says = 0,

    /// <summary>
    /// A diagnostic code the generator has to report, and how many times.
    /// </summary>
    Reports = 1
}
