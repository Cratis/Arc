// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents what a model graph is being validated for, so rules scoped to it (for example
/// <c>BaseValidator&lt;T&gt;.WhenCommand</c> and <c>WhenQuery</c>) apply.
/// </summary>
public enum ValidationTargetKind
{
    /// <summary>
    /// The graph is a command.
    /// </summary>
    Command = 0,

    /// <summary>
    /// The graph is the arguments of a query.
    /// </summary>
    Query = 1
}
