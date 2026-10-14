// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Queries;

namespace Cratis.Arc.Screenplay.for_QueryReader;

public class when_selecting_queries
{
    [Theory]
    [InlineData(false, "public static Order Create(object value) => new();", true)]
    [InlineData(false, "public static Order Helper(Order current, object e) => current;", false)]
    [InlineData(false, "public static Order? Reduce(object e, Order? current) => current;", false)]
    [InlineData(false, "public static Task<Order?> Helper(object e, object context, Order? current) => Task.FromResult(current);", false)]
    [InlineData(false, "public static Order Helper(System.Collections.Generic.IEnumerable<Order> models) => new();", true)]
    [InlineData(false, "private static Order Helper() => new();", true)]
    [InlineData(true, "public static Order Helper() => new();", false)]
    [InlineData(true, "private static Order Helper() => new();", false)]
    [InlineData(true, "public static Task BeginReplay(object service) => Task.CompletedTask;", false)]
    [InlineData(true, "[Query] internal static Task<Order> Helper() => Task.FromResult(new Order());", true)]
    [InlineData(true, "[Query] public static Order? Helper() => null;", true)]
    [InlineData(false, "public static ValueTask<Order> Helper() => new(new Order());", true)]
    [InlineData(true, "[Query] public static ValueTask<Order> Helper() => new(new Order());", true)]
    public void should_preserve_legacy_rules_and_select_explicit_queries(bool explicitMode, string method, bool expected)
    {
        var type = TypeOf(method, explicitMode);
        var queries = QueryReader.MethodsOf(type).ToArray();
        queries.Length.ShouldEqual(expected ? 2 : 1);
        queries.Any(_ => _.Name == "ById").ShouldBeTrue();
    }

    [Fact]
    public void should_select_before_resolving_overloads() => QueryReader.MethodsOf(TypeOf("public static Order ById(int id) => new();", true)).Single().Parameters[0].Type.SpecialType.ShouldEqual(Microsoft.CodeAnalysis.SpecialType.System_String);

    [Fact]
    public void should_reject_invalid_marked_task_helpers() => Catch.Exception(() => _ = QueryReader.MethodsOf(TypeOf("[Query] public static Task BeginReplay(object service) => Task.CompletedTask;", true))).ShouldNotBeNull();

    [Fact]
    public void should_reject_marked_self_parameters() => Catch.Exception(() => _ = QueryReader.MethodsOf(TypeOf("[Query] public static Order? Reduce(object e, Order? current) => current;", false))).ShouldNotBeNull();

    static Microsoft.CodeAnalysis.INamedTypeSymbol TypeOf(string method, bool explicitMode)
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $$"""
            #nullable enable
            using System.Threading.Tasks;
            using Cratis.Arc.Queries.ModelBound;
            namespace Library.Feature.Slice;
            [ReadModel(RequireQueryAttribute = {{explicitMode.ToString().ToLowerInvariant()}})]
            public partial record Order
            {
                [Query] public static Order ById(string id) => new();
            }
            public partial record Order
            {
                {{method}}
            }
            """));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        return compilation.GetTypeByMetadataName("Library.Feature.Slice.Order")!;
    }
}
