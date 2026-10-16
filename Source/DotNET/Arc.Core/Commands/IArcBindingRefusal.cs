// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Marks an Arc-owned binding refusal whose message is safe to show to a caller.
/// </summary>
internal interface IArcBindingRefusal
{
    /// <summary>
    /// Describes the refusal for the normalized member.
    /// </summary>
    /// <param name="member">The affected member.</param>
    /// <returns>The client-facing refusal.</returns>
    string MessageFor(string member);
}
