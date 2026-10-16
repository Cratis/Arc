// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class DateComparisonSources
{
    internal static string Guard(string type, string comparison) => $$"""
        using System;
        using System.Globalization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Lending.Reserving;
        [EventType] public record BookReserved(string Title);
        [EventType] public record ReservationRefused(string Title);
        [Command] public record ReserveBook([Cratis.Chronicle.Keys.Key] string Id, string Title, {{type}} Start, {{type}} End)
        {
            public object Handle() => {{comparison}} ? new BookReserved(Title) : new ReservationRefused(Title);
        }
        """;

    internal static string Validator(string type, string rule, bool concept = false) => $$"""
        using System;
        using System.Globalization;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        using FluentValidation;
        namespace Library.Lending.Reserving;
        public record ReservationDate({{type.TrimEnd('?')}} Value) : ConceptAs<{{type.TrimEnd('?')}}>(Value);
        [EventType] public record BookReserved(string Title);
        [Command] public record ReserveBook([Cratis.Chronicle.Keys.Key] string Id, string Title, {{type}} Value, {{type}} Other, ReservationDate Date)
        {
            public BookReserved Handle() => new(Title);
        }
        public class DateValidator : {{(concept ? "ConceptValidator<ReservationDate>" : "CommandValidator<ReserveBook>")}}
        {
            public DateValidator() => RuleFor(value => value.Value).{{rule}};
        }
        """;
}
