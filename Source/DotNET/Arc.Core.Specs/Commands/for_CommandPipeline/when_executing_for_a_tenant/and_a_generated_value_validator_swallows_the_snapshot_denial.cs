// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_generated_value_validator_swallows_the_snapshot_denial : given.a_command_pipeline_with_denied_snapshot_membership
{
    CommandResult _result;
    ReadingGeneratedValueValidator _validator;

    void Establish()
    {
        var validators = Substitute.For<IDiscoverableValidators>();
        _validator = new ReadingGeneratedValueValidator();
        validators.TryGet(typeof(Id), Arg.Any<IServiceProvider>(), out Arg.Any<IValidator>())
            .Returns(call =>
            {
                call[2] = _validator;
                return true;
            });
        _serviceProvider.GetService(typeof(IDiscoverableValidators)).Returns(validators);

        // An independent resolver failure must not hide the swallowed denial behind an ordinary error result.
        UseTheRealArgumentResolver(
            typeof(Generating),
            typeof(Generating).GetMethod(nameof(Generating.Handle))!,
            CommandResult.Error(_correlationId, "Independent argument resolution failure"));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_run_the_validator() => _validator.Ran.ShouldBeTrue();
    [Fact] void should_catch_the_snapshot_denial() => _validator.CaughtDenial.ShouldBeTrue();
    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_execute_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    public sealed record Id(string Value) : ConceptAs<string>(Value), IGeneratable<Id>
    {
        static Id IGeneratable<Id>.New() => new("generated");
    }

    public class Generating
    {
        public void Handle([GeneratedValue] Id id) { }
    }

    class ReadingGeneratedValueValidator : AbstractValidator<Id>
    {
        public bool Ran { get; private set; }
        public bool CaughtDenial { get; private set; }

        public ReadingGeneratedValueValidator() => RuleFor(_ => _).Custom((_, context) =>
        {
            Ran = true;
            try
            {
                context.GetTenant();
            }
            catch (TenantMembershipDenied)
            {
                CaughtDenial = true;
            }
        });
    }
}
