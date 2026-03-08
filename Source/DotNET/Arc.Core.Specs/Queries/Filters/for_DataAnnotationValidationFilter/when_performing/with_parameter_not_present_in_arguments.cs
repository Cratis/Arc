// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_parameter_not_present_in_arguments : given.a_data_annotation_validation_filter
{
    [AlwaysFails]
    public record RequiredId(int Value);

    async Task Because()
    {
        EstablishPerformerWith(
            new QueryParameters([new QueryParameter("id", typeof(RequiredId))]),
            QueryArguments.Empty);

        _result = await _filter.OnPerform(_context);
    }

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_run_validation() => _result.ValidationResults.ShouldBeEmpty();
}
