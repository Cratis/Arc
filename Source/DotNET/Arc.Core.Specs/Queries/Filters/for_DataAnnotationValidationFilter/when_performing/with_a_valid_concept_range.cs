// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_a_valid_concept_range : given.a_data_annotation_validation_filter
{
    void Establish() => EstablishPerformerWith(
        new QueryParameters([new QueryParameter("Amount", typeof(Amount))]),
        new QueryArguments { ["Amount"] = new Amount(50) });

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_accept_the_value() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_validation_errors() => _result.ValidationResults.ShouldBeEmpty();
}
