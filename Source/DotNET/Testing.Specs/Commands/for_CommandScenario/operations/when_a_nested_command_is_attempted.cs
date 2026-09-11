// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations;

public class when_a_nested_command_is_attempted : Specification
{
    CommandScenario<Parent> _scenario;
    ChildBusinessWork _work;
    CommandResult _result;

    void Establish()
    {
        _work = new();
        _scenario = new();
        _scenario.Services.AddSingleton(_work);
        _scenario.Services.AddSingleton(Substitute.For<given.ICapacityReservations>());
    }

    async Task Because() => _result = await _scenario.Execute(new Parent());
    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_reject_before_child_business_execution() => _work.Called.ShouldBeFalse();
    [Fact] void should_reject_parent_operations_even_if_the_child_result_is_ignored() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_no_started_operations() => _scenario.ShouldHaveNoOperationInvocations();

    [Command]
    public record Parent
    {
        public async Task<CommandOperations> Handle(ICommandPipeline pipeline)
        {
            await pipeline.Execute(new Child());
            return [new given.ReserveCapacity(Guid.Empty, "warehouse", 1)];
        }
    }

    [Command]
    public record Child
    {
        public void Handle(ChildBusinessWork work) => work.Called = true;
    }

    public class ChildBusinessWork
    {
        public bool Called { get; set; }
    }
}
