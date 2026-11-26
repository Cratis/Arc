// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_validating_an_absent_optional_key : given.a_keyed_protected_read
{
    FluentValidation.Results.ValidationResult _result;
    Balance? _balance;

    async Task Because()
    {
        SelectKey(string.Empty);
        _context = _context with { Type = typeof(Assign), Command = new Assign(new AccountId(string.Empty)) };
        var types = Substitute.For<Cratis.Types.ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(AssignValidator)]);
        var validators = new DiscoverableValidators(types);
        using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Assign));
        CommandTransaction.Current = _unit;
        using var frame = _unit.EnforceReadProtection();
        CommandDecisionReads.Begin(typeof(Assign));
        try
        {
            validators.TryGet(typeof(Assign), _services, out var validator).ShouldBeTrue();
            _balance = ((AssignValidator)validator).Balance;
            _result = await ((AssignValidator)validator).ValidateAsync((Assign)_context.Command);
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_construct_the_validator_with_null_state() => _balance.ShouldBeNull();
    [Fact] void should_return_the_field_level_message() => _result.Errors.Single().ErrorMessage.ShouldEqual("Account is required.");
    [Fact] void should_attach_the_error_to_the_key_field() => _result.Errors.Single().PropertyName.ShouldEqual("account");
    [Fact] void should_not_acquire_state() => _inner.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_the_sink() => _stored.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_enroll_a_guard() => _unit.HasEnrolledDecisionReads.ShouldBeFalse();

    [ReadProtection(ReadProtectionMode.On)]
    public record Assign(AccountId? Account);

    public class AssignValidator : CommandValidator<Assign>
    {
        public AssignValidator([LookUpBy(nameof(Assign.Account))] Balance? balance)
        {
            Balance = balance;
            RuleFor(command => command.Account!).NotEmpty().WithMessage("Account is required.");
        }

        public Balance? Balance { get; }
    }
}
