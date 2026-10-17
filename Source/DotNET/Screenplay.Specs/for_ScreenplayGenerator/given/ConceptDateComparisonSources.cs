// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class ConceptDateComparisonSources
{
    internal static string Guard(string type, string comparison, bool optional = false) =>
        "#nullable enable\n" + DateComparisonSources.Guard(optional ? "ReservationDate?" : "ReservationDate", comparison) + "\n" + Concept(type);

    internal static string Validator(string type, string rule) =>
        DateComparisonSources.Validator(type, rule).Replace($"{type} Value, {type} Other", "ReservationDate Value, ReservationDate Other", StringComparison.Ordinal)
            .Replace($"public record ReservationDate({type} Value) : ConceptAs<{type}>(Value);", Concept(type), StringComparison.Ordinal);

    static string Concept(string type) => $$"""
        public record ReservationDate({{type}} Value) : Cratis.Concepts.ConceptAs<{{type}}>(Value)
        {
            public static implicit operator ReservationDate({{type}} value) => new(value);
        }
        """;
}
