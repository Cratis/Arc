// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_parameter_value_that_fails_validation : given.a_data_annotation_validation_filter
{
    [AlwaysFails]
    public record BrokenId(int Value);

    void Establish() => EstablishPerformerWith(
        new QueryParameters([new QueryParameter("id", typeof(BrokenId))]),
        new QueryArguments { ["id"] = new BrokenId(1) });

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_be_invalid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_include_the_parameter_name_in_the_result() => _result.ValidationResults.First().Members.ShouldContain("id");
}
