// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class without_registered_concept_members : given.annotated_concepts
{
    readonly List<System.ComponentModel.DataAnnotations.ValidationResult> _expected = [];

    void Establish() => Command(new LegacyPlainText("long"));

    async Task Because()
    {
#pragma warning disable CA1849, MA0042 // TryValidateObjectAsync exists on net11.0 only.
        Validator.TryValidateObject(_context.Command, new ValidationContext(_context.Command), _expected, true);
#pragma warning restore CA1849, MA0042
        _result = await _filter.OnExecution(_context);
    }

    [Fact] void should_match_the_validator_messages() => _result.ValidationResults.Select(result => result.Message).ShouldEqual(_expected.Select(result => result.ErrorMessage));
    [Fact] void should_match_the_validator_members() => _result.ValidationResults.Single().Members.ShouldEqual(_expected.Single().MemberNames.Select(name => name.ToLowerInvariant()));
}
