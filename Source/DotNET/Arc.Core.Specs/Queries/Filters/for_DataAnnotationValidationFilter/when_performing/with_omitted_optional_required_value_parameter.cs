// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_omitted_optional_required_value_parameter : given.annotated_parameters
{
    public static int Query([Required] int input = 10) => input;

    void Establish() => OmittedParameter(nameof(Query));
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_accept_the_omitted_argument() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_validation_errors() => _result.ValidationResults.ShouldBeEmpty();
}
