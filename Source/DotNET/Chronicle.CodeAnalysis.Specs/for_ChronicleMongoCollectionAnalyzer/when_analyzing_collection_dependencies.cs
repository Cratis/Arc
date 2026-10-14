// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture concatenates independently readable snippets.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ChronicleMongoCollectionAnalyzer;

public class when_analyzing_collection_dependencies
{
    /// <summary>
    /// Only the generic collection identity is needed; query execution is not part of this analyzer.
    /// </summary>
    const string Collection = "namespace MongoDB.Driver { public interface IMongoCollection<T>; }";

    [Fact]
    public async Task should_advise_for_a_model_bound_projection() =>
        await Verify("[Command] public record Change(OwnerId Id) { public Changed Handle(MongoDB.Driver.IMongoCollection<State> states) => new(); }",
            Info().WithArguments("states", "State", "not proven"));

    [Fact]
    public async Task should_advise_for_a_constructor_dependency() =>
        await Verify("public class Reader(MongoDB.Driver.IMongoCollection<State> states);", Info());

    [Fact]
    public async Task should_advise_for_a_fluent_projection_target() =>
        await Verify("public record Projected(OwnerId Id); public class Projection : IProjectionFor<Projected> { public void Define(IProjectionBuilderFor<Projected> builder) => builder.From<Changed>(); } public class Reader(MongoDB.Driver.IMongoCollection<Projected> states);", Info());

    [Fact]
    public async Task should_advise_for_a_reducer_target() =>
        await Verify("public record Reduced(OwnerId Id); public class Reducer : IReducerFor<Reduced>; public class Reader(MongoDB.Driver.IMongoCollection<Reduced> states);", Info());

    [Fact]
    public async Task should_advise_for_a_self_reducing_model() =>
        await Verify("public record Reduced(OwnerId Id) { public static Reduced Reduce(Changed @event, Reduced? current) => new(new OwnerId(Guid.NewGuid())); } public class Reader(MongoDB.Driver.IMongoCollection<Reduced> states);", Info());

    [Fact]
    public async Task should_ignore_an_ordinary_mongo_model() =>
        await Verify("public class Reader(MongoDB.Driver.IMongoCollection<Plain> states);");

    [Fact]
    public async Task should_ignore_a_plain_injected_model() =>
        await Verify("[Command] public record Change(OwnerId Id) { public Changed Handle(State state) => new(); }");

    [Fact]
    public async Task should_ignore_an_unrelated_collection_with_the_same_short_name() =>
        await Verify("public interface IMongoCollection<T>; public class Reader(IMongoCollection<State> states);");

    [Fact]
    public async Task should_ignore_a_lambda_parameter() =>
        await Verify("public class Reader { public void Read() { Func<MongoDB.Driver.IMongoCollection<State>, bool> check = states => true; } }");

    static DiagnosticBuilder Info() => AnalyzerVerifier<ChronicleMongoCollectionAnalyzer>.Diagnostic("ARCCHR0026").WithSeverity(DiagnosticSeverity.Info);
    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) =>
        AnalyzerVerifier<ChronicleMongoCollectionAnalyzer>.VerifyAnalyzerAsync(DecisionAnalyzerSource.Definitions + Collection + source, diagnostics);
}
