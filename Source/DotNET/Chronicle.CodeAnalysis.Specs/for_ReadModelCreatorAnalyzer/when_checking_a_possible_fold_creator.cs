// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReadModelCreatorAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReadModelCreatorAnalyzer;

#pragma warning disable MA0136 // Compiler fixture behavior is independent of line endings.

public class when_checking_a_possible_fold_creator
{
    [Theory]
    [InlineData("public static Order {|#0:Create|}(Unmarked e) => new();", "Create")]
    [InlineData("public static Order {|#0:Create|}(Placed e) => new();", "Create")]
    public async Task should_warn_without_changing_selection(string method, string name) => await VerifyCS.VerifyAnalyzerAsync(Source(method), Warning(name));

    [Theory]
    [InlineData("public static Order Create(string id) => new();")]
    [InlineData("public static Order Create(OrderId id) => new();")]
    [InlineData("public static Order Reduce(Placed e, Order current) => current;")]
    [InlineData("public static Order? Reduce(Placed e, Order? current) => current;")]
    [InlineData("public static Task<Order?> Reduce(Placed e, Order? current, EventContext context) => Task.FromResult(current);")]
    [InlineData("public static Order Create(Placed e, Order current) => current;")]
    [InlineData("public static Task BeginReplay(object service) => Task.CompletedTask;")]
    [InlineData("public static Order From(Placed e) => new();")]
    [InlineData("private static Order Create(Unmarked e) => new();")]
    [InlineData("internal static Task<Order> Create(Placed e) => Task.FromResult(new Order());")]
    public async Task should_ignore_other_shapes(string method) => await VerifyCS.VerifyAnalyzerAsync(Source(method));

    [Fact]
    public async Task should_ignore_helpers_in_explicit_mode() => await VerifyCS.VerifyAnalyzerAsync(Source("public static Order Create(Placed e) => new();").Replace("[ReadModel]", "[ReadModel(RequireQueryAttribute = true)]", StringComparison.Ordinal));

    internal static string Source(string method) => $$"""
        #nullable enable
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace TestApp
        {
            [EventType] public record Placed;
            public record Unmarked;
            public record OrderId(string Value) : ConceptAs<string>(Value);
            [ReadModel]
            public record Order
            {
                {{method}}
            }
        }
        """;

    internal static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.ExpectedDiagnostic Warning(string name = "Create") =>
        VerifyCS.Diagnostic("ARCCHR0019").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Order", name);
}
