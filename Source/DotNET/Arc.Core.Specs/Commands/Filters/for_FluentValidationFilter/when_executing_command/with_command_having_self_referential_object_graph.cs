// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_command_having_self_referential_object_graph : given.a_fluent_validation_filter
{
    CommandResult _result;
    Exception _exception;

    void Establish()
    {
        var node = new Node("root");
        node.Child = new Node("child") { Parent = node }; // node <-> child cycle

        var command = new CommandWithGraph(node);
        _context = new CommandContext(_correlationId, typeof(CommandWithGraph), command, [], new());
        _discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>()).Returns(false);
    }

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _filter.OnExecution(_context));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    record CommandWithGraph(Node Root);

    /// <summary>
    /// A mutable node whose <see cref="Parent"/> and <see cref="Child"/> form a reference cycle —
    /// the same shape that JsonNode's parent back-reference produces.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    public class Node(string name)
    {
        public string Name => name;
        public Node? Parent { get; set; }
        public Node? Child { get; set; }
    }
}
