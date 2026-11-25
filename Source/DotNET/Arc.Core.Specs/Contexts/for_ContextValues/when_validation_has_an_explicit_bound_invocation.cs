// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_validation_has_an_explicit_bound_invocation : given.an_execution_source
{
    IValidator _validator;
    ValidatorInvoker _invoker;
    object _value;
    object _bound;
    object _nested;
    object _restored;
    object _outside;
    CancellationToken _expectedCancellation;
    CancellationToken _boundCancellation;

    void Establish()
    {
        _invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
        _validator = Substitute.For<IValidator>();
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _value = await call.Arg<IValidationContext>().ProvidedValues().GetAsync("value");
            return new FluentValidation.Results.ValidationResult();
        });
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        _expectedCancellation = cancellation.Token;
        using (OperationSnapshot.Begin(_services, () => null))
        {
            await new Commands.Filters.AvailabilityFilter().OnExecution(_command);
            await _invoker.Invoke("a nested property", _validator, "property", cancellation.Token);
            _bound = _value;
            _boundCancellation = _readCancellation;
            using (OperationSnapshot.Begin(_services, () => null))
            {
                await _invoker.Invoke("no bound invocation", _validator, "property");
                _nested = _value;
            }

            await _invoker.Invoke("another property", _validator, "property");
            _restored = _value;
        }

        await _invoker.Invoke("outside an operation", _validator, "property");
        _outside = _value;
    }

    [Fact] void should_read_the_bound_value() => _bound.ShouldEqual("bound");
    [Fact] void should_pass_the_root_command_not_the_property() => _reads[0].Arguments.ShouldBeSame(_command.Command);
    [Fact] void should_reuse_the_gates_invocation_for_all_properties() => _reads.TrueForAll(invocation => ReferenceEquals(invocation, _gated)).ShouldBeTrue();
    [Fact] void should_honor_the_validation_cancellation_token() => _boundCancellation.ShouldEqual(_expectedCancellation);
    [Fact] void should_not_inherit_an_invocation_into_a_nested_snapshot() => _nested.ShouldBeNull();
    [Fact] void should_restore_the_explicit_outer_invocation() => _restored.ShouldEqual("bound");
    [Fact] void should_not_infer_an_invocation_outside_an_operation() => _outside.ShouldBeNull();
    [Fact] void should_only_read_execution_values_for_the_bound_invocation() => _reads.Count.ShouldEqual(2);
}
