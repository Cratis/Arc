// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpToolMapper.given;

public class a_mapper_context : a_catalog_context
{
    protected McpToolDescriptor Map(McpExposureDeclaration declaration) =>
        McpToolMapper.Map(declaration, _options, _handlers.Values.ToArray(), _performers);

    protected McpToolDescriptor MapCommand<TCommand>(string name = "a_tool") =>
        Map(new McpExposureDeclaration(McpToolKind.Command, typeof(TCommand), name, "A tool."));

    protected McpToolDescriptor MapQuery(string fullyQualifiedName, string name = "a_tool") =>
        Map(new McpExposureDeclaration(McpToolKind.Query, fullyQualifiedName, name, "A tool."));

    protected void AddScopedPerformer()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName(nameof(ScopedReadModel.Reports)));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName(ScopedQueryName));
        performer.ReadModelType.Returns(typeof(ScopedReadModel));
        performer.Type.Returns(typeof(ScopedReadModel));
        performer.Parameters.Returns(QueryParameters.Empty);
        _performers.Add(performer);
    }

    protected string ScopedQueryName => $"{typeof(ScopedReadModel).FullName}.{nameof(ScopedReadModel.Reports)}";
}
