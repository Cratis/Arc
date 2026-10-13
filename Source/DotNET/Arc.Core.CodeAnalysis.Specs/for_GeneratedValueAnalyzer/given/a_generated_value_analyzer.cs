// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.CodeAnalysis;

using Verify = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.GeneratedValueAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.given;

public class a_generated_value_analyzer
{
    protected const string Types = @"
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;

        public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
        {
            public static CartId New() => new(GenerateValue.Uuid());
        }

        public readonly record struct TicketId(Guid Value) : IGeneratable<TicketId>
        {
            public static TicketId New() => new(GenerateValue.Uuid());
        }
        ";

    protected static Task VerifyDiagnostic(string source, string id, params string[] arguments) =>
        VerifySource(source, Verify.Diagnostic(id)
            .WithSeverity(id == "ARC0025" || id == "ARC0027" ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error)
            .WithLocation(0).WithArguments(arguments));

    protected static Task VerifyValid(string source) => VerifySource(source);

    protected static async Task VerifyRuntimeParity(string source)
    {
        var fullSource = $"{Types}\n{source}";
        var compilation = await TestProject.CreateProject(SourceMarker.Parse(fullSource).Source).GetCompilationAsync();
        await using var output = new MemoryStream();
        var emitted = compilation!.Emit(output);
        emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();
        var command = Assembly.Load(output.ToArray()).GetType("Allocate")!;
        var error = Catch.Exception(() => _ = new ModelBoundCommandHandler(command, command.GetHandleMethod()));
        if (error is InvalidGeneratedValueParameter)
        {
            await VerifyDiagnostic(source, "ARC0022", "value");
        }
        else
        {
            error.ShouldBeNull();
            await VerifyValid(source);
        }
    }

    static async Task VerifySource(string source, params ExpectedDiagnostic[] expected)
    {
        var fullSource = $"{Types}\n{source}";
        var compilation = await TestProject.CreateProject(SourceMarker.Parse(fullSource).Source).GetCompilationAsync();
        compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();
        await Verify.VerifyAnalyzerAsync(fullSource, expected);
    }
}
