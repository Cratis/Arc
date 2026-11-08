// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpToolCeiling.given;

public class a_ceiling_context : Specification
{
    protected McpExposure _exposure;
    protected McpOptions _options;
    protected ICommandHandlerProviders _commands;
    protected IQueryPerformerProviders _queries;
    protected Dictionary<Type, ICommandHandler> _handlers;
    protected List<IQueryPerformer> _performers;

    protected McpToolCeiling _ceiling;
    protected Exception? _error;

    void Establish()
    {
        _exposure = new McpExposure();
        _options = new McpOptions { ApplicationName = "app" };
        _handlers = [];
        _performers = [];
        _commands = Substitute.For<ICommandHandlerProviders>();
        _commands.Handlers.Returns(_ => _handlers.Values);
        _queries = Substitute.For<IQueryPerformerProviders>();
        _queries.Performers.Returns(_ => _performers);

        AddHandlerFor<DeclaredCommand>();
        AddHandlerFor<UndeclaredCommand>();
        AddHandlerFor<ApprovalRequiredCommand>();
        AddHandlerFor<DerivedFromApprovalRequiredCommand>();
        AddPerformerFor(nameof(ReadModelWithQueries.Everything));
        AddPerformerFor(nameof(ReadModelWithQueries.NotExposed));
    }

    protected void AddHandlerFor<TCommand>()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(TCommand));
        _handlers[typeof(TCommand)] = handler;
    }

    protected void AddPerformerFor(string method)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName(method));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"{typeof(ReadModelWithQueries).FullName}.{method}"));
        performer.ReadModelType.Returns(typeof(ReadModelWithQueries));
        _performers.Add(performer);
    }

    protected string QueryName(string method) => $"{typeof(ReadModelWithQueries).FullName}.{method}";

    protected void Build() => _error = Catch.Exception(() => _ceiling = McpToolCeiling.Build(_exposure, _options, _commands, _queries));

    protected void ShouldNotHaveDispatched() => _handlers.Values.ToList().ForEach(_ => _.DidNotReceiveWithAnyArgs().Handle(default!));
}
