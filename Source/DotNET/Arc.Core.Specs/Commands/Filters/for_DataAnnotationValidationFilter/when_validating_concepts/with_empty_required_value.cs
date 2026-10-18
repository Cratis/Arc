// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_empty_required_value : given.annotated_concepts
{
    void Establish() => Command(new RequiredText(new Text(string.Empty)));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_reject_the_empty_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_use_the_display_name() => _result.ValidationResults.Single().Message.ShouldEqual("The Label field is required.");
    [Fact] void should_preserve_member_normalization() => _result.ValidationResults.Single().Members.ShouldContainOnly("name");
}
