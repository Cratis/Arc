// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Queries;

namespace Cratis.Arc.Screenplay.for_QueryReader;

public class when_reading_fold_creators
{
    [Theory]
    [InlineData("public static Order Create(Placed e) => new();", false)]
    [InlineData("public static Order Create(Derived e) => new();", false)]
    [InlineData("public static Order Create(EventContext context) => new();", false)]
    [InlineData("public static Order Create(EventContext? context) => new();", false)]
    [InlineData("public static Task<Order> Create(Placed e) => Task.FromResult(new Order());", false)]
    [InlineData("public static ValueTask<Order> Create(Placed e) => ValueTask.FromResult(new Order());", false)]
    [InlineData("[NotAQuery] public static Order Create(Unmarked e) => new();", false)]
    [InlineData("private static Order Create(string id) => new();", false)]
    [InlineData("public static Order Create(OrderId id) => new();", true)]
    [InlineData("public static Order Create(Unmarked e) => new();", true)]
    [InlineData("public static Order From(Placed e) => new();", true)]
    [InlineData("internal static Order Create(string id) => new();", true)]
    [InlineData("public static Order Create(Placed e, int first, int second) => new();", true)]
    [InlineData("public static IEnumerable<Order> Create(Placed e) => [];", true)]
    public void should_include_only_real_queries(string method, bool expected)
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Cratis.Arc.Queries.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Concepts;
            namespace Library.Feature.Slice;
            [EventType] public record Placed;
            public record Derived : Placed;
            public record Unmarked;
            public record OrderId(string Value) : ConceptAs<string>(Value);
            [ReadModel]
            public record Order
            {
                public static Order ById(string id) => new();
                {{method}}
            }
            """));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var type = compilation.GetTypeByMetadataName("Library.Feature.Slice.Order")!;
        var queries = QueryReader.MethodsOf(type).ToArray();
        queries.Length.ShouldEqual(expected ? 2 : 1);
        queries.Any(_ => _.Name == "ById").ShouldBeTrue();
    }
}
