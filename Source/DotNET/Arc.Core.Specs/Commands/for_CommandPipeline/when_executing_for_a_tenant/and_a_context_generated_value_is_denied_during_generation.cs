// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_for_a_tenant;

public class and_a_context_generated_value_is_denied_during_generation : given.a_command_pipeline_with_denied_snapshot_membership
{
    CommandResult _result;

    void Establish()
    {
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
}
