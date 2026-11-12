// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp.for_McpExposure;

public class when_exposing_a_query_by_read_model_type : a_catalog_context
{
    McpToolDescriptor? _top;
    McpToolDescriptor? _nestedDotted;
    McpToolDescriptor? _nestedPlus;

    void Establish()
    {
        AddNestedPerformer($"{typeof(Holder.NestedReadModel).FullName!.Replace('+', '.')}.{nameof(Holder.NestedReadModel.All)}", nameof(Holder.NestedReadModel.All));
        AddNestedPerformer($"{typeof(Holder.NestedReadModel).FullName}.{nameof(Holder.NestedReadModel.Other)}", nameof(Holder.NestedReadModel.Other));

        _exposure
            .ExposeQuery<ReadModelWithQueries>(nameof(ReadModelWithQueries.Everything), "everything", "All of it.")
            .ExposeQuery<Holder.NestedReadModel>(nameof(Holder.NestedReadModel.All), "nested_all", "All nested.")
            .ExposeQuery<Holder.NestedReadModel>(nameof(Holder.NestedReadModel.Other), "nested_other", "Other nested.");
    }

    void Because()
    {
        Build();
        _catalog.TryGet("everything", out _top);
        _catalog.TryGet("nested_all", out _nestedDotted);
        _catalog.TryGet("nested_other", out _nestedPlus);
    }

    [Fact] void should_build_the_catalog() => _error.ShouldBeNull();
    [Fact] void should_hold_the_query_as_a_read_tool() => (_top!.Kind, _top.Access).ShouldEqual((McpToolKind.Query, McpToolAccess.Read));
    [Fact] void should_hold_the_same_query_as_the_named_form() => _top!.Operation.ShouldEqual(new FullyQualifiedQueryName(QueryName(nameof(ReadModelWithQueries.Everything))));
    [Fact] void should_find_a_nested_read_model_named_with_dots() => _nestedDotted.ShouldNotBeNull();
    [Fact] void should_find_a_nested_read_model_named_with_a_plus() => _nestedPlus.ShouldNotBeNull();

    void AddNestedPerformer(string fullyQualifiedName, string method)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName(method));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName(fullyQualifiedName));
        performer.ReadModelType.Returns(typeof(Holder.NestedReadModel));
        performer.Type.Returns(typeof(Holder.NestedReadModel));
        performer.Parameters.Returns(new QueryParameters([]));
        _performers.Add(performer);
    }

    public static class Holder
    {
        public class NestedReadModel
        {
            public string Value { get; init; } = string.Empty;

            public static IEnumerable<NestedReadModel> All() => [];

            public static IEnumerable<NestedReadModel> Other() => [];
        }
    }
}
