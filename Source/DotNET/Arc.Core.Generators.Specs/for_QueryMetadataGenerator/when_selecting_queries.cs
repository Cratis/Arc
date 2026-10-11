// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator;

public class when_selecting_queries
{
    [Theory]
    [InlineData(false, "public static Order Helper() => new();", true)]
    [InlineData(false, "[Query] public static Order Helper() => new();", true)]
    [InlineData(true, "public static Order Helper() => new();", false)]
    [InlineData(true, "internal static Order Helper() => new();", false)]
    [InlineData(true, "private static Order Helper() => new();", false)]
    [InlineData(true, "public Order Helper() => new();", false)]
    [InlineData(true, "public static Task<Order> Helper() => Task.FromResult(new Order());", false)]
    [InlineData(true, "public static Task BeginReplay(object service) => Task.CompletedTask;", false)]
    [InlineData(true, "public static Task EndReplay(object service) => Task.CompletedTask;", false)]
    [InlineData(true, "public static Task BeginReplayPartition(string partition, object service) => Task.CompletedTask;", false)]
    [InlineData(true, "public static Task EndReplayPartition(string partition, object service) => Task.CompletedTask;", false)]
    [InlineData(true, "[Query] public static Order? Helper() => null;", true)]
    [InlineData(true, "[Query] internal static Task<Order> Helper() => Task.FromResult(new Order());", true)]
    [InlineData(true, "[Query] public static ISubject<IEnumerable<Order>> Helper() => null!;", true)]
    public void should_preserve_defaults_and_select_only_marked_explicit_queries(bool explicitMode, string method, bool expected)
    {
        var result = GeneratorTestHelper.RunGenerator(Source(method, explicitMode));
        var generated = GeneratorTestHelper.GetGeneratedSourceByHintName(result, "GeneratedQueryMetadata.g.cs");
        result.Diagnostics.ShouldBeEmpty();
        generated.Contains($"TestApp.Order.{(method.Contains("BeginReplay", StringComparison.Ordinal) ? "BeginReplay" : method.Contains("EndReplay", StringComparison.Ordinal) ? "EndReplay" : "Helper")}", StringComparison.Ordinal).ShouldEqual(expected);
        generated.ShouldContain("TestApp.Order.ById");
    }

    [Theory]
    [InlineData("[Query] private static Order Helper() => new();")]
    [InlineData("[Query] public Order Helper() => new();")]
    [InlineData("[Query] public static Order Helper<T>(T value) => new();")]
    [InlineData("[Query] public static Task Helper() => Task.CompletedTask;")]
    [InlineData("[Query] public static string Helper() => string.Empty;")]
    [InlineData("[Query] public static Order Helper(ref int value) => new();")]
    public void should_reject_invalid_markers_before_filtering(string method)
    {
        var result = GeneratorTestHelper.RunGenerator(Source(method, true));
        result.Diagnostics.Any(_ => _.Id == "ARC0026").ShouldBeTrue();
    }

    [Fact]
    public void should_choose_a_marked_overload_before_registering_its_name()
    {
        var result = GeneratorTestHelper.RunGenerator(Source("public static Order ById(int id) => new();", true));
        result.Diagnostics.ShouldBeEmpty();
        GeneratorTestHelper.GetGeneratedSourceByHintName(result, "GeneratedQueryMetadata.g.cs").Split("TestApp.Order.ById", StringSplitOptions.None).Length.ShouldEqual(2);
    }

    [Fact]
    public void should_reject_two_selected_overloads()
    {
        var result = GeneratorTestHelper.RunGenerator(Source("[Query] public static Order ById(int id) => new();", true));
        result.Diagnostics.Count(_ => _.Id == "ARC0026").ShouldEqual(2);
    }

    [Fact]
    public void should_report_invalid_markers_before_selected_name_collisions()
    {
        var result = GeneratorTestHelper.RunGenerator(Source("[Query] private static Order Helper() => new(); [Query] public static Order ById(int id) => new();", true));
        result.Diagnostics.Length.ShouldEqual(3);
        result.Diagnostics[0].GetMessage().ShouldContain("Query 'Helper'");
    }

    [Theory]
    [InlineData(false, true, 2)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, 0)]
    public void should_select_across_files_and_allow_removing_the_last_marker(bool explicitMode, bool marked, int expected)
    {
        var first = $"using Cratis.Arc.Queries.ModelBound; namespace CrossFile; [ReadModel(RequireQueryAttribute = {explicitMode.ToString().ToLowerInvariant()})] public partial record Order {{ public static Order Sibling() => new(); }}";
        var second = $"using Cratis.Arc.Queries.ModelBound; namespace CrossFile; public partial record Order {{ {(marked ? "[Query]" : string.Empty)} public static Order All() => new(); }}";
        var result = GeneratorTestHelper.RunGenerator([first, second]);
        result.Diagnostics.ShouldBeEmpty();
        GeneratorTestHelper.GetGeneratedSourceByHintName(result, "GeneratedQueryMetadata.g.cs").Split("CrossFile.Order.", StringSplitOptions.None).Length.ShouldEqual(expected + 1);
    }

    internal static string Source(string method, bool explicitMode) => $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Reactive.Subjects;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        namespace TestApp;
        [ReadModel(RequireQueryAttribute = {{explicitMode.ToString().ToLowerInvariant()}})]
        public partial class Order
        {
            [Query] public static Order ById(string id) => new();
        }
        public partial class Order
        {
            {{method}}
        }
        """;
}
