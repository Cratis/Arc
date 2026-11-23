// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_CommandOperationGenerator;

public class when_generating_native_union_result_factories : Specification
{
    string _source;
    Diagnostic[] _diagnostics;
    Assembly _assembly;

    void Because()
    {
        var compilation = CSharpCompilation.Create(
            "NativeUnionResultGeneratorSpec",
            [CSharpSyntaxTree.ParseText(
                """
                using System;
                using System.Threading.Tasks;
                using Cratis.Arc.Commands.ModelBound;
                namespace NativeResults;
                public record Receipt(string Number);
                public record Recorded(string Name);
                // Compiler-generated union structs; equality is not meaningful for a handler return.
                #pragma warning disable CA1815
                public readonly union Answer(Receipt, int);
                public readonly union Generic<T>(T, bool);
                public readonly union Nested(Answer, (Guid, Recorded));
                #pragma warning restore CA1815
                [Command] public record Direct { public Answer Handle() => 42; }
                [Command] public record Awaited { public Task<Nested> Handle() => Task.FromResult(new Nested(new Answer(42))); }
                [Command] public record ValueAwaited { public ValueTask<Generic<decimal>> Handle() => new(new Generic<decimal>(1m)); }
                """,
                new CSharpParseOptions(LanguageVersion.Preview))],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(CommandResult).Assembly.Location)
                .Append(typeof(CorrelationId).Assembly.Location)
                .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create([new CommandOperationGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview))
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        _source = driver.GetRunResult().Results.Single().GeneratedSources.Single(source => source.HintName == "CommandResults.g.cs").SourceText.ToString();
        _diagnostics = [.. generatorDiagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)];
        using var binary = new MemoryStream();
        var emitted = output.Emit(binary);
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
        }

        _assembly = Assembly.Load(binary.ToArray());
        RuntimeHelpers.RunModuleConstructor(_assembly.ManifestModule.ModuleHandle);
    }

    [Fact] void should_compile_without_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_reference_case() => _source.ShouldContain("typeof(global::NativeResults.Receipt)");
    [Fact] void should_register_the_primitive_case() => _source.ShouldContain("typeof(int)");
    [Fact] void should_register_the_tuple_elements() => _source.ShouldContain("typeof(global::NativeResults.Recorded)");
    [Fact] void should_register_closed_generic_union_cases() => _source.ShouldContain("typeof(decimal)");
    [Fact] void should_not_register_the_named_union_wrapper() => _source.ShouldNotContain("typeof(global::NativeResults.Answer)");
    [Fact] void should_not_register_the_nested_union_wrapper() => _source.ShouldNotContain("typeof(global::NativeResults.Nested)");
    [Fact] void should_not_register_the_generic_union_wrapper() => _source.ShouldNotContain("typeof(global::NativeResults.Generic");
    [Fact] void should_dispatch_the_case_to_the_generated_assembly() => CommandResultFactories.For(_assembly.GetType("NativeResults.Receipt")!)!.Method.DeclaringType!.Assembly.ShouldEqual(_assembly);
}
#endif
