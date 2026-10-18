// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_empty_required_concept : given.annotated_concepts
{
    void Establish() => ConceptParameter("name", new Text(string.Empty), value => ((Text)value).Value, new RequiredAttribute());
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_use_the_display_name() => _result.ValidationResults.Single().Message.ShouldEqual("The Label field is required.");
    [Fact] void should_identify_the_parameter() => _result.ValidationResults.Single().Members.ShouldContainOnly("name");
}
