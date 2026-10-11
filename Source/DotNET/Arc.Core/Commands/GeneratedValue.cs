// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents a validated generated value captured during command argument resolution.
/// </summary>
/// <param name="Order">The zero-based resolution order within the command.</param>
/// <param name="Name">The generated value name.</param>
/// <param name="Type">The declared value type.</param>
/// <param name="Value">The generated value. Do not log or trace it.</param>
public sealed record GeneratedValue(int Order, string Name, Type Type, object Value)
{
    /// <summary>
    /// Formats the capture metadata without exposing the generated value.
    /// </summary>
    /// <returns>The order, name and declared type of the generated value.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder($"{nameof(GeneratedValue)} {{ ");
        PrintMembers(builder);
        builder.Append(" }");

        return builder.ToString();
    }

    bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(Order)} = {Order}, {nameof(Name)} = {Name}, {nameof(Type)} = {Type}");

        return true;
    }
}
