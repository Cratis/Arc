// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_LiteralConverter;

/// <summary>
/// The printer formats anything that is not a double with the current culture, which emits a comma as the decimal
/// separator on most of the world's machines and produces a document that no longer parses. Everything numeric has
/// to reach the printer as a double.
/// </summary>
public class when_converting_a_number : Specification
{
    readonly ScreenplayNaming _naming = new();
    CultureInfo _original;
    LiteralExpressionSyntax _fromDecimal;
    LiteralExpressionSyntax _fromInt;
    LiteralExpressionSyntax _fromFloat;
    string _printed;

    void Establish()
    {
        _original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
    }

    void Because()
    {
        _fromDecimal = LiteralConverter.Convert(5.5m, _naming);
        _fromInt = LiteralConverter.Convert(42, _naming);
        _fromFloat = LiteralConverter.Convert(1.25f, _naming);
        _printed = Print(_fromDecimal);
    }

    void Destroy() => CultureInfo.CurrentCulture = _original;

    [Fact] void should_convert_a_decimal_to_a_double() => _fromDecimal.Value.ShouldBeOfExactType<double>();
    [Fact] void should_convert_an_int_to_a_double() => _fromInt.Value.ShouldBeOfExactType<double>();
    [Fact] void should_convert_a_float_to_a_double() => _fromFloat.Value.ShouldBeOfExactType<double>();
    [Fact] void should_print_a_fraction_with_an_invariant_separator() => _printed.Contains("5.5", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_print_a_fraction_with_the_current_culture_separator() => _printed.Contains("5,5", StringComparison.Ordinal).ShouldBeFalse();

    static string Print(ExpressionSyntax value)
    {
        var slice = new SliceSyntax(
            SliceType.StateChange,
            "Reserving",
            [],
            [
                new CommandSyntax(
                    "ReserveBook",
                    [],
                    null,
                    [],
                    [new ProducesSyntax("BookReserved", null, [new PropertyMappingSyntax("total", value, SourceLocation.Start)], SourceLocation.Start)],
                    null,
                    SourceLocation.Start)
            ],
            [],
            null,
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var module = new ModuleSyntax("Library", [], [new FeatureSyntax("Lending", [], [slice], SourceLocation.Start)], SourceLocation.Start);

        return new ScreenplayPrinter().Print(new ApplicationSyntax([], [], [], [module], SourceLocation.Start));
    }
}
