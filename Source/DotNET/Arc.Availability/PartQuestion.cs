// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents asking whether one part is available for the data it is about.
/// </summary>
/// <param name="Part">The full name of the command type, or of the type that declares the query.</param>
/// <param name="Arguments">The arguments of the command or query, by name, as the gate reads them through <c>arguments.name</c>. Matching ignores case.</param>
/// <remarks>
/// Arguments are untrusted input from the caller. They feed the same gate that decides an execution; they never grant
/// anything and never replace tenant or identity.
/// </remarks>
public record PartQuestion(string Part, IReadOnlyDictionary<string, object?> Arguments)
{
    /// <summary>
    /// Creates a <see cref="PartQuestion"/> from the arguments as a JSON object.
    /// </summary>
    /// <param name="part">The full name of the part.</param>
    /// <param name="arguments">The arguments as a JSON object; empty or null for none.</param>
    /// <returns>The <see cref="PartQuestion"/>.</returns>
    /// <exception cref="ArgumentException">The arguments are not a JSON object.</exception>
    public static PartQuestion From(string part, string? arguments)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(arguments);
                if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                {
                    throw new ArgumentException("The arguments must be a JSON object.", nameof(arguments));
                }

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    values[property.Name] = ToValue(property.Value);
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new ArgumentException("The arguments must be a JSON object.", nameof(arguments), ex);
            }
        }

        return new(part, values);
    }

    static object? ToValue(System.Text.Json.JsonElement element) => element.ValueKind switch
    {
        System.Text.Json.JsonValueKind.String => element.GetString(),
        System.Text.Json.JsonValueKind.Number => element.TryGetInt64(out var integer) ? (object)integer : element.GetDouble(),
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Object => element.EnumerateObject().ToDictionary(_ => _.Name, _ => ToValue(_.Value), StringComparer.OrdinalIgnoreCase),
        System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(ToValue).ToArray(),
        _ => null
    };
}
