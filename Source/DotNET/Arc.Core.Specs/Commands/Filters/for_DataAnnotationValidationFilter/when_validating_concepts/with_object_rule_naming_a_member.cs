// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_object_rule_naming_a_member : given.annotated_concepts
{
    void Establish() => Command(new MemberObjectRule(new Text("valid")));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_preserve_the_object_rule() => _result.ValidationResults.Single().Message.ShouldEqual("Object member rejected");
    [Fact] void should_preserve_the_member() => _result.ValidationResults.Single().Members.ShouldContainOnly("name");
}
