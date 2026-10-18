// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_non_concept_property : given.annotated_concepts
{
    CommandResult _legacyResult;
    CommandContext _legacyContext;

    void Establish()
    {
        Command(new PlainText("long"));
        _legacyContext = new(_correlationId, typeof(LegacyPlainText), new LegacyPlainText("long"), [], new());
    }

    async Task Because()
    {
        _result = await _filter.OnExecution(_context);
        _legacyResult = await _filter.OnExecution(_legacyContext);
    }

    [Fact] void should_preserve_the_message() => _result.ValidationResults.Single().Message.ShouldEqual(_legacyResult.ValidationResults.Single().Message);
    [Fact] void should_preserve_the_members() => _result.ValidationResults.Single().Members.ShouldEqual(_legacyResult.ValidationResults.Single().Members);
    [Fact] void should_preserve_the_severity() => _result.ValidationResults.Single().Severity.ShouldEqual(_legacyResult.ValidationResults.Single().Severity);
}
