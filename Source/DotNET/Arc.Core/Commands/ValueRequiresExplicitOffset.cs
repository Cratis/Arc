// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a date-time input does not contain an explicit offset or Z.
/// </summary>
public sealed class ValueRequiresExplicitOffset() : JsonException("The value requires an explicit offset or Z."), IArcBindingRefusal
{
    /// <inheritdoc/>
    /// <remarks>
    /// "DateTime" is the Screenplay primitive name for an instant and deliberately applies to <see cref="DateTimeOffset"/> properties too,
    /// matching the Screenplay contract text. Do not change it to "DateTimeOffset".
    /// </remarks>
    string IArcBindingRefusal.MessageFor(string member) => $"DateTime command property '{member}' requires an explicit offset or Z.";
}
