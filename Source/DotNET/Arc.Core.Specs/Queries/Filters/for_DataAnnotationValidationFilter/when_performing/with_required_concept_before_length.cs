// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_required_concept_before_length : given.annotated_concepts
{
    void Establish() => ConceptParameter("name", new Text(string.Empty), value => ((Text)value).Value, new MinLengthAttribute(3), new RequiredAttribute());
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_report_only_the_required_error() => _result.ValidationResults.Single().Message.ShouldEqual("The Label field is required.");
}
