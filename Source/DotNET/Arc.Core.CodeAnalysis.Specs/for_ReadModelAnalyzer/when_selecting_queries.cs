// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

#pragma warning disable MA0136 // Compiler fixture behavior is independent of line endings.

public class when_selecting_queries
{
    [Theory]
    [InlineData("public static Order Helper() => new();")]
    [InlineData("internal static Order Helper() => new();")]
    [InlineData("private static Order Helper() => new();")]
    [InlineData("public Order Helper() => new();")]
    [InlineData("public static Task<Order> Helper() => Task.FromResult(new Order());")]
    [InlineData("public static bool Helper() => false;")]
    [InlineData("public static string Helper() => string.Empty;")]
    [InlineData("public static Order Helper<T>(T value) => new();")]
    [InlineData("public static Task BeginReplay(object service) => Task.CompletedTask;")]
    [InlineData("public static Task EndReplay(object service) => Task.CompletedTask;")]
    [InlineData("public static Task BeginReplayPartition(string partition, object service) => Task.CompletedTask;")]
    [InlineData("public static Task EndReplayPartition(string partition, object service) => Task.CompletedTask;")]
    [InlineData("[Query] public static Order? Helper() => null;")]
    [InlineData("[Query] internal static Task<Order> Helper() => Task.FromResult(new Order());")]
    [InlineData("[Query] public static ISubject<IEnumerable<Order>> Helper() => null!;")]
    [InlineData("[Query] public static Task<ISubject<IEnumerable<Order>>> Helper() => null!;")]
    public async Task should_ignore_unmarked_helpers_without_query_diagnostics(string method) => await VerifyCS.VerifyAnalyzerAsync(Source(method));

    [Theory]
    [InlineData("[Query] private static Order {|#0:Helper|}() => new();")]
    [InlineData("[Query] public Order {|#0:Helper|}() => new();")]
    [InlineData("[Query] public static Order {|#0:Helper|}<T>(T value) => new();")]
    [InlineData("[Query] public static Task {|#0:Helper|}() => Task.CompletedTask;")]
    [InlineData("[Query] public static string {|#0:Helper|}() => string.Empty;")]
    [InlineData("[Query] public static Order {|#0:Helper|}(ref int value) => new();")]
    [InlineData("[Query] public static ISubject<Task<Order>> {|#0:Helper|}() => null!;")]
    [InlineData("[Query] public static Task<ISubject<Task<Order>>> {|#0:Helper|}() => null!;")]
    [InlineData("[Query] public static Task<Task<Order>> {|#0:Helper|}() => null!;")]
    public async Task should_reject_invalid_marked_methods(string method) => await VerifyCS.VerifyAnalyzerAsync(Source(method), Invalid("Helper"));

    [Fact]
    public async Task should_reject_a_marker_without_a_read_model_owner() => await VerifyCS.VerifyAnalyzerAsync(Source("[Query] public static Order {|#0:Helper|}() => new();", "").Replace("[Query] public static Order ById", "public static Order ById", StringComparison.Ordinal), Invalid("Helper"));

    [Fact]
    public async Task should_preserve_legacy_signature_diagnostics_when_a_sibling_is_marked() => await VerifyCS.VerifyAnalyzerAsync(
        Source("public static string {|#0:Helper|}() => string.Empty;", "[ReadModel]"),
        VerifyCS.Diagnostic("ARC0001").WithLocation(0).WithArguments("Helper", "Order", "string"));

    [Fact]
    public async Task should_not_treat_the_grpc_marker_as_an_arc_query() => await VerifyCS.VerifyAnalyzerAsync(Source("[Cratis.Chronicle.Infrastructure.Grpc.Query] public static string Helper() => string.Empty;") + "\nnamespace Cratis.Chronicle.Infrastructure.Grpc { public class QueryAttribute : System.Attribute { } }");

    internal static string Source(string method, string attribute = "[ReadModel(RequireQueryAttribute = true)]") => $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Reactive.Subjects;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        namespace TestApp
        {
            {{attribute}}
            public partial class Order
            {
                [Query] public static Order ById(string id) => new();
            }
            public partial class Order
            {
                {{method}}
            }
        }
        """;

    static Cratis.Arc.CodeAnalysis.Specs.Testing.ExpectedDiagnostic Invalid(string name) => VerifyCS.Diagnostic("ARC0026").WithLocation(0).WithArguments(name,
        "[Query] requires a public/internal static nongeneric ordinary method with value parameters and a supported read-model return shape on [ReadModel]; correct the declaration or remove [Query]");
}
