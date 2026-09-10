// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations;

public class when_legacy_object_commands_are_nested : Specification
{
    CommandScenario<Parent> _scenario;
    CommandResult _result;

    void Establish() => _scenario = new();
    async Task Because() => _result = await _scenario.Execute(new Parent());
    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_keep_existing_nested_execution() => _result.ShouldBeSuccessful();
    [Fact] void should_return_the_child_response() => ((CommandResult<string>)_result).Response.ShouldEqual("child");
    [Fact] void should_not_opt_into_operation_recovery() => _result.Recovery.ShouldBeNull();

    [Command]
    public record Parent
    {
        public async Task<object> Handle(ICommandPipeline pipeline) => (await pipeline.Execute<string>(new Child())).Response!;
    }

    [Command]
    public record Child
    {
        public object Handle() => "child";
    }
}
