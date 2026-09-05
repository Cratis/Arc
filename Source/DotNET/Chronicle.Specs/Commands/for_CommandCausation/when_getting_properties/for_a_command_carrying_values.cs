// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties.given;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties;

/// <summary>
/// Naming the command tells a reader which command produced an event but not which invocation of it, so two orders
/// raised by the same command are indistinguishable on the chain. Recording the values is what separates them.
/// </summary>
public class for_a_command_carrying_values : Specification
{
    record SubmitExpenseReport(
        ExpenseReportId ReportId,
        Amount Total,
        ExpenseCategory Category,
        string Description,
        bool Reimbursable,
        DateTimeOffset SubmittedAt);

    static readonly Guid _reportId = Guid.NewGuid();
    static readonly DateTimeOffset _submittedAt = new(2026, 2, 26, 11, 3, 0, TimeSpan.Zero);

    IDictionary<string, string> _properties;

    void Because() => _properties = CommandCausation.PropertiesFor(
        typeof(SubmitExpenseReport),
        new SubmitExpenseReport(
            new(_reportId),
            new(1234.56m),
            ExpenseCategory.Travel,
            "Flights to the customer",
            true,
            _submittedAt));

    [Fact] void should_still_name_the_command() =>
        _properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(SubmitExpenseReport));

    [Fact] void should_key_values_by_the_camel_cased_property_name() =>
        _properties.ContainsKey("reportId").ShouldBeTrue();

    [Fact] void should_record_the_value_a_concept_wraps_rather_than_the_wrapper() =>
        _properties["reportId"].ShouldEqual(_reportId.ToString());

    [Fact] void should_record_a_numeric_concept_invariantly() =>
        _properties["total"].ShouldEqual(1234.56m.ToString(CultureInfo.InvariantCulture));

    [Fact] void should_record_an_enum_by_name() =>
        _properties["category"].ShouldEqual(nameof(ExpenseCategory.Travel));

    [Fact] void should_record_a_string_as_written() =>
        _properties["description"].ShouldEqual("Flights to the customer");

    [Fact] void should_record_a_boolean() =>
        _properties["reimbursable"].ShouldEqual("true");

    [Fact] void should_record_a_date_round_trippably() =>
        _properties["submittedAt"].ShouldEqual(_submittedAt.ToString("O", CultureInfo.InvariantCulture));
}
