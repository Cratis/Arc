// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_invalid_paging : given.a_query_pipeline
{
    Paging _paging;
    QueryResult _result;

    void Establish()
    {
        _paging = new Paging(0, -1, true);

        var validator = new PageSizeValidator();
        _discoverableValidators.TryGet(typeof(PageSize), out Arg.Any<IValidator>()).Returns(callInfo =>
        {
            callInfo[1] = validator;
            return true;
        });
    }

    async Task Because() => _result = await _pipeline.Perform("SomeQuery", [], _paging, Sorting.None, _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_a_validation_error() => _result.ValidationResults.ShouldNotBeEmpty();
}
