// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReadModelCreatorAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReadModelCreatorAnalyzer;

public class when_checking_a_possible_fold_creator
{
    [Theory]
    [InlineData("Unmarked", "", true)]
    [InlineData("IOrderEvent", "", true)]
    [InlineData("DerivedUnmarked", "", true)]
    [InlineData("Placed", "", false)]
    [InlineData("Derived", "", false)]
    [InlineData("Unmarked", "[NotAQuery]", false)]
    [InlineData("string", "", false)]
    [InlineData("int", "", false)]
    [InlineData("object", "", true)]
    [InlineData("OrderId", "", false)]
    [InlineData("EventContext", "", false)]
    public async Task should_warn_only_for_unrecognized_event_parameters(string parameter, string attribute, bool expected)
    {
        var source = Source($"{attribute} public static Order {{|#0:Create|}}({parameter} e) => new();");
        await VerifyCS.VerifyAnalyzerAsync(source, expected ? [Warning()] : []);
    }

    [Theory]
    [InlineData("public static Order From(Unmarked e) => new();")]
    [InlineData("public static Order Create(Unmarked e, EventContext context) => new();")]
    [InlineData("public static Order Create(Unmarked e, EventContext? context) => new();")]
    [InlineData("public static Order Create(Unmarked e, int a, int b) => new();")]
    [InlineData("public static string Create(Unmarked e) => string.Empty;")]
    [InlineData("private static Order Create(Unmarked e) => new();")]
    [InlineData("public Order Create(Unmarked e) => new();")]
    public async Task should_ignore_methods_without_the_unexcluded_creator_shape(string method) => await VerifyCS.VerifyAnalyzerAsync(Source(method));

    [Theory]
    [InlineData("internal static Order {|#0:Create|}(Unmarked e) => new();")]
    [InlineData("public static Task<Order> {|#0:Create|}(Unmarked e) => Task.FromResult(new Order());")]
    [InlineData("public static ValueTask<Order> {|#0:Create|}(IOrderEvent e) => ValueTask.FromResult(new Order());")]
    public async Task should_warn_for_internal_and_awaitable_creators(string method) => await VerifyCS.VerifyAnalyzerAsync(Source(method), Warning());

#pragma warning disable MA0136 // Compiler fixture behavior is independent of line endings.
    internal static string Source(string method) => $$"""
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace TestApp
        {
            [EventType] public record Placed;
            public record Derived : Placed;
            public record Unmarked;
            public record DerivedUnmarked : Unmarked;
            public interface IOrderEvent { }
            public record OrderId(string Value) : ConceptAs<string>(Value);
            [ReadModel]
            public record Order
            {
                {{method}}
            }
        }
        """;
#pragma warning restore MA0136

    internal static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.ExpectedDiagnostic Warning() =>
        VerifyCS.Diagnostic("ARCCHR0019").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Order");
}
