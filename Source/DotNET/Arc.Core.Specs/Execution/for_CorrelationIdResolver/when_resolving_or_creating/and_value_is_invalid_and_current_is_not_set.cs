// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Execution.for_CorrelationIdResolver.when_resolving_or_creating;

public class and_value_is_invalid_and_current_is_not_set : given.a_correlation_id_resolver
{
    CorrelationId _result;

    void Because() => _result = CorrelationIdResolver.ResolveOrCreate("not-a-guid", _correlationIdAccessor);

    [Fact] void should_generate_a_new_correlation_id() => _result.ShouldNotEqual(CorrelationId.NotSet);
}
