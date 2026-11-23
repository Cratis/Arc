// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_query_validation_has_an_explicit_bound_invocation : given.an_execution_source
{
    object _value;

    async Task Because()
    {
        var validator = Substitute.For<IValidator>();
        validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _value = await call.Arg<IValidationContext>().ProvidedValues().GetAsync("value");
            return new FluentValidation.Results.ValidationResult();
        });
        using var snapshot = OperationSnapshot.Begin(_services, () => null);
        await new Queries.Filters.AvailabilityFilter(_performers).OnPerform(_query);
        await new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance).Invoke(42, validator, "count");
    }

    [Fact] void should_read_the_bound_value() => _value.ShouldEqual("bound");
    [Fact] void should_reuse_the_gates_invocation() => _reads.Single().ShouldBeSame(_gated);
    [Fact] void should_pass_the_bound_query_arguments_not_the_validated_scalar() => _reads.Single().Arguments.ShouldBeSame(_query.Arguments);
    [Fact] void should_keep_the_pinned_receipt() => _reads.Single().ReceivedAt.ShouldEqual(Receipt);
}
