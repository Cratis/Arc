// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Execution.for_CorrelationIdResolver.when_resolving_or_creating;

public class and_value_is_valid : given.a_correlation_id_resolver
{
    CorrelationId _expectedCorrelationId;
    CorrelationId _result;

    void Establish() => _expectedCorrelationId = CorrelationId.New();

    void Because() => _result = CorrelationIdResolver.ResolveOrCreate(_expectedCorrelationId.ToString(), _correlationIdAccessor);

    [Fact] void should_return_the_provided_correlation_id() => _result.ShouldEqual(_expectedCorrelationId);
}
