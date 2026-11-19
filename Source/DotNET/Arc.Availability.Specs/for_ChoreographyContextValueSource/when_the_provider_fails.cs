// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Contexts;
using Cratis.Execution;

namespace Cratis.Arc.Availability.for_ChoreographyContextValueSource;

public class when_the_provider_fails : given.a_value_host
{
    Exception _error;
    CommandResult _result;

    async Task Because()
    {
        _error = await Catch.Exception(async () => await ValuesFor(null).GetAsync("broken"));
        _result = CommandResult.Error(CorrelationId.New(), _error);
    }

    [Fact] void should_fail_with_the_unavailable_error() => _error.ShouldBeOfExactType<ContextValueUnavailableException>();
    [Fact] void should_keep_the_cause() => _error.InnerException.ShouldNotBeNull();
    [Fact] void should_be_retryable() => _result.IsRetryable.ShouldBeTrue();
    [Fact] void should_not_be_a_denial() => _result.IsAuthorized.ShouldBeTrue();
}
