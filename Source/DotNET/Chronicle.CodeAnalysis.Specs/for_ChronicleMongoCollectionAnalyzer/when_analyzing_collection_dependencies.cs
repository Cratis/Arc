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
    public async Task should_warn_under_on() =>
        await Verify("[Command, Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)] public record Change(OwnerId Id) { public Changed Handle(MongoDB.Driver.IMongoCollection<State> states) => new(); }",
            AnalyzerVerifier<ChronicleMongoCollectionAnalyzer>.Diagnostic("ARCCHR0026").WithSeverity(DiagnosticSeverity.Warning));

    [Fact]
    public async Task should_warn_for_a_validator_dependency_under_on() =>
        await Verify("[Command, Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)] public record Change { public Changed Handle() => new(); } public class Validator(MongoDB.Driver.IMongoCollection<State> states) : Cratis.Arc.Commands.CommandValidator<Change>;",
            AnalyzerVerifier<ChronicleMongoCollectionAnalyzer>.Diagnostic("ARCCHR0026").WithSeverity(DiagnosticSeverity.Warning));

    [Fact]
    public async Task should_advise_for_a_constructor_dependency() =>
        await Verify("[Command] public record Change { public Changed Handle() => new(); } public class Validator : Cratis.Arc.Commands.CommandValidator<Change> { public Validator(MongoDB.Driver.IMongoCollection<State> states) { } }", Info());

    [Fact]
    public async Task should_advise_for_a_fluent_projection_target() =>
        await Verify("public record Projected(OwnerId Id); public class Projection : IProjectionFor<Projected> { public void Define(IProjectionBuilderFor<Projected> builder) => builder.From<Changed>(); } [Command] public record Change { public Changed Handle(MongoDB.Driver.IMongoCollection<Projected> states) => new(); }", Info());

    [Fact]
    public async Task should_advise_for_a_reducer_target() =>
        await Verify("public record Reduced(OwnerId Id); public class Reducer : IReducerFor<Reduced>; [Command] public record Change { public Changed Handle(MongoDB.Driver.IMongoCollection<Reduced> states) => new(); }", Info());

    [Fact]
    public async Task should_advise_for_a_self_reducing_model() =>
        await Verify("public record Reduced(OwnerId Id) { public static Reduced Reduce(Changed @event, Reduced? current) => new(new OwnerId(Guid.NewGuid())); } [Command] public record Change { public Changed Handle(MongoDB.Driver.IMongoCollection<Reduced> states) => new(); }", Info());

    [Fact]
    public async Task should_advise_for_a_validator_primary_constructor() =>
        await Verify("[Command] public record Change { public Changed Handle() => new(); } public class Validator(MongoDB.Driver.IMongoCollection<State> states) : Cratis.Arc.Commands.CommandValidator<Change>;", Info());

    [Fact]
    public async Task should_advise_for_a_non_public_provide_dependency() =>
        await Verify("[Command] public record Change { State Provide(MongoDB.Driver.IMongoCollection<State> states) => new(new OwnerId(Guid.NewGuid())); public Changed Handle(State state) => new(); }", Info());

    [Theory]
    [InlineData("[Cratis.Arc.Queries.ModelBound.ReadModel] public record Query { public static State Read(MongoDB.Driver.IMongoCollection<State> states) => new(new OwnerId(Guid.NewGuid())); }")]
    [InlineData("[Cratis.Arc.Queries.ModelBound.ReadModel] public class Query(MongoDB.Driver.IMongoCollection<State> states);")]
    [InlineData("public class Reader(MongoDB.Driver.IMongoCollection<State> states);")]
    [InlineData("public class Reader { public State Read(MongoDB.Driver.IMongoCollection<State> states) => new(new OwnerId(Guid.NewGuid())); }")]
    [InlineData("[Command] public record Change { public Changed Handle() => new(); public void Helper(MongoDB.Driver.IMongoCollection<State> states) { } }")]
    [InlineData("[Command] public record Change(MongoDB.Driver.IMongoCollection<State> states) { public Changed Handle() => new(); }")]
    [InlineData("[Command] public record Change { public Changed Handle() => new(); public static State Provide(MongoDB.Driver.IMongoCollection<State> states) => new(new OwnerId(Guid.NewGuid())); }")]
    [InlineData("[Command] public record Change { public Changed Handle() => new(); } public class Validator : Cratis.Arc.Commands.CommandValidator<Change> { public void Check(MongoDB.Driver.IMongoCollection<State> states) { } }")]
    public async Task should_ignore_parameters_outside_command_injection_sites(string source) =>
        await Verify(source);

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
