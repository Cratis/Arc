// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_class_rule_without_members : given.annotated_concepts
{
    void Establish() => Command(new TypeRule(new Text("valid")));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_preserve_the_class_rule() => _result.ValidationResults.Single().Message.ShouldEqual("Type rejected");
    [Fact] void should_keep_the_empty_member_list() => _result.ValidationResults.Single().Members.ShouldBeEmpty();
}
