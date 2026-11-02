// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_generated_value_validator_swallows_the_snapshot_denial : given.a_command_pipeline_with_denied_snapshot_membership
{
    CommandResult _result;

    void Establish()
    {
        var validators = Substitute.For<IDiscoverableValidators>();
        IValidator validator = new ReadingGeneratedValueValidator();
        validators.TryGet(typeof(ContextId), Arg.Any<IServiceProvider>(), out Arg.Any<IValidator>())
            .Returns(call =>
            {
                call[2] = validator;
                return true;
            });
        _serviceProvider.GetService(typeof(IDiscoverableValidators)).Returns(validators);
        UseTheRealArgumentResolver(typeof(Generating), typeof(Generating).GetMethod(nameof(Generating.Handle))!);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_execute_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    public sealed record ContextId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<ContextId>
    {
        static ContextId IGeneratableInContext<ContextId>.New(GenerationContext context) => new(context.Tenant);
    }

    public class Generating
    {
        public void Handle([GeneratedValue] ContextId id) { }
    }

    class ReadingGeneratedValueValidator : AbstractValidator<ContextId>
    {
        public ReadingGeneratedValueValidator() => RuleFor(_ => _).Custom((_, context) => context.GetTenant());
    }
}
