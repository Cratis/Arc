// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryResult;

public class when_creating_a_not_ready_result : Specification
{
    CorrelationId _correlationId;
    QueryResult _result;

    void Establish() => _correlationId = CorrelationId.New();

    void Because() => _result = QueryResult.NotReady(_correlationId);

    [Fact] void should_not_be_ready() => _result.IsReady.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
