// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_proxies_with_documented_members : Specification
{
    string _query = null!;
    string _observableQuery = null!;
    string _command = null!;

    void Because()
    {
        var readModelType = typeof(SimpleReadModel);
        var query = new QueryDescriptor(
            readModelType,
            readModelType.GetMethod("GetById")!,
            "/api/queries/simple-read-model/get-by-id",
            "GetById",
            "SimpleReadModel",
            "SimpleReadModel",
            false,
            false,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [new RequestParameterDescriptor(typeof(Guid), "Id", "Guid", "Guid", false, Documentation: "The ID to find.")],
            [],
            [],
            [readModelType],
            null,
            [],
            false,
            []);
        _query = InMemoryProxyGenerator.GenerateQuery(query);
        _observableQuery = InMemoryProxyGenerator.GenerateQuery(query with { IsObservable = true });

        var commandType = typeof(SimpleCommand);
        var command = new CommandDescriptor(
            commandType,
            commandType.GetMethod("Handle")!,
            "/api/commands/simple-command",
            "SimpleCommand",
            [commandType.GetProperty("Name")!.ToPropertyDescriptor() with { Documentation = "The name." }],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            false,
            ModelDescriptor.Empty,
            [],
            null,
            [],
            false,
            []);
        _command = InMemoryProxyGenerator.GenerateCommand(command);
    }

    [Fact] void should_not_emit_whitespace_only_lines_in_query() => HasWhitespaceOnlyLine(_query).ShouldBeFalse();
    [Fact] void should_not_emit_whitespace_only_lines_in_observable_query() => HasWhitespaceOnlyLine(_observableQuery).ShouldBeFalse();
    [Fact] void should_not_emit_whitespace_only_lines_in_command() => HasWhitespaceOnlyLine(_command).ShouldBeFalse();

    static bool HasWhitespaceOnlyLine(string code) => code.Split('\n').Any(line => line.TrimEnd('\r').Length > 0 && string.IsNullOrWhiteSpace(line));
}
