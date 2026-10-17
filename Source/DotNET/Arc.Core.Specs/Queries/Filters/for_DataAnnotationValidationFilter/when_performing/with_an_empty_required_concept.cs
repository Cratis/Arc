// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_an_empty_required_concept : given.a_data_annotation_validation_filter
{
    void Establish() => EstablishPerformerWith(
        new QueryParameters([new QueryParameter("Name", typeof(Name))]),
        new QueryArguments { ["Name"] = new Name(string.Empty) });

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_client_field_once() => _result.ValidationResults.Single().Members.ShouldContainOnly("name");
}
