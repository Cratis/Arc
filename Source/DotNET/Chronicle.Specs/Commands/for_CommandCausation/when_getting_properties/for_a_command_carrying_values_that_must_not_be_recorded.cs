// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties.given;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties;

/// <summary>
/// The causation is written into the event log and stays there for as long as the events do, so a value withheld
/// here is withheld permanently and a value recorded here is recorded permanently. Both markings are honoured: what
/// GDPR describes, through Chronicle's own compliance metadata, and what it does not - a password is not personal
/// data, and nothing about <c>[PII]</c> would keep it out.
/// </summary>
public class for_a_command_carrying_values_that_must_not_be_recorded : Specification
{
    record ChangeClaimantDetails(
        ExpenseReportId ReportId,
        [property: PII("The name of a person")] string DirectlyMarkedName,
        ClaimantName NameThroughAPiiConcept,
        [PII("Marked on the positional parameter")] string MarkedOnTheParameter,
        [property: NotAudited] string Password,
        [NotAudited] string ApiKey);

    static readonly string[] _withheldValues = ["Jane Doe", "hunter2", "sk-live-0000"];

    IDictionary<string, string> _properties;

    void Because() => _properties = CommandCausation.PropertiesFor(
        typeof(ChangeClaimantDetails),
        new ChangeClaimantDetails(
            new(Guid.NewGuid()),
            "Jane Doe",
            new("Jane Doe"),
            "Jane Doe",
            "hunter2",
            "sk-live-0000"));

    [Fact] void should_record_the_value_that_is_neither_personal_nor_secret() =>
        _properties.ContainsKey("reportId").ShouldBeTrue();

    [Fact] void should_withhold_a_property_marked_as_personal_data() =>
        _properties.ContainsKey("directlyMarkedName").ShouldBeFalse();

    [Fact] void should_withhold_a_value_whose_concept_is_marked_as_personal_data() =>
        _properties.ContainsKey("nameThroughAPiiConcept").ShouldBeFalse();

    [Fact] void should_withhold_a_positional_parameter_marked_as_personal_data() =>
        _properties.ContainsKey("markedOnTheParameter").ShouldBeFalse();

    [Fact] void should_withhold_a_property_marked_as_not_audited() =>
        _properties.ContainsKey("password").ShouldBeFalse();

    [Fact] void should_withhold_a_positional_parameter_marked_as_not_audited() =>
        _properties.ContainsKey("apiKey").ShouldBeFalse();

    [Fact] void should_never_leak_a_withheld_value_under_any_key() =>
        _properties.Values.Any(value => _withheldValues.Contains(value, StringComparer.Ordinal)).ShouldBeFalse();
}
