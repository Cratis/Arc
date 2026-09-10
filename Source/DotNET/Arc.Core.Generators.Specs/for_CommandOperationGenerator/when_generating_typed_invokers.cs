// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_CommandOperationGenerator;

public class when_generating_typed_invokers : Specification
{
    string _source;
    Diagnostic[] _diagnostics;
    int _executed;
    int _compensated;
    bool _usesGeneratedCalls;

    async Task Because()
    {
        var compilation = CSharpCompilation.Create(
            "OperationGeneratorSpec",
            [CSharpSyntaxTree.ParseText("""
                using Cratis.Arc.Commands;
                using System.Threading;
                using System.Threading.Tasks;
                public interface IReservations { }
                public sealed record Reserve(string Key) : ICommandOperation
                {
                    public static int Executed;
                    public static int Compensated;
                    public Task Execute(IReservations reservations, CancellationToken token) { Executed++; return Task.CompletedTask; }
                    public ValueTask Compensate(IReservations reservations, CommandOperationFailure failure, CancellationToken token) { Compensated++; return ValueTask.CompletedTask; }
                }
                public sealed record Clear : ICommandOperation
                {
                    public void Execute() { }
                }
                """)],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(ICommandOperation).Assembly.Location)
                .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(new CommandOperationGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        _source = driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString();
        _diagnostics = [.. generatorDiagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
        await using var binary = new MemoryStream();
        output.Emit(binary).Success.ShouldBeTrue();
        var assembly = Assembly.Load(binary.ToArray());
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        var operationType = assembly.GetType("Reserve")!;
        var operation = (ICommandOperation)Activator.CreateInstance(operationType, "ownership-key")!;

        // Inspect the internal registry seam to prove the compiled module registered its typed calls, rather than
        // accidentally passing this test via reflection fallback. Production callers only return declarations.
        var get = typeof(CommandOperationInvokers).GetMethod("Get", BindingFlags.Static | BindingFlags.NonPublic)!;
        var invoker = (CommandOperationInvoker)get.Invoke(null, [operationType])!;
        var execute = (CommandOperationMethod)typeof(CommandOperationInvoker).GetProperty("Execute", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(invoker)!;
        var compensate = (CommandOperationMethod)typeof(CommandOperationInvoker).GetProperty("Compensate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(invoker)!;
        _usesGeneratedCalls = execute.Method.DeclaringType!.Assembly == assembly && compensate.Method.DeclaringType!.Assembly == assembly;
        await execute(operation, [null, CancellationToken.None]);
        await compensate(operation, [null, null, CancellationToken.None]);
        _executed = (int)operationType.GetField("Executed")!.GetValue(null)!;
        _compensated = (int)operationType.GetField("Compensated")!.GetValue(null)!;
    }

    [Fact] void should_emit_compilable_invokers() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_use_direct_typed_execution() => _source.ShouldContain("((global::Reserve)operation).Execute");
    [Fact] void should_use_direct_typed_compensation() => _source.ShouldContain("((global::Reserve)operation).Compensate");
    [Fact] void should_include_failure_context_in_preflight_metadata() => _source.ShouldContain("typeof(global::Cratis.Arc.Commands.CommandOperationFailure)");
    [Fact] void should_support_void_without_compensation() => _source.ShouldContain("((global::Clear)operation).Execute()");
    [Fact] void should_register_at_assembly_load() => _source.ShouldContain("ModuleInitializer");
    [Fact] void should_dispatch_to_the_generated_assembly_not_reflection_fallback() => _usesGeneratedCalls.ShouldBeTrue();
    [Fact] void should_run_the_compiled_execute() => _executed.ShouldEqual(1);
    [Fact] void should_run_the_compiled_compensate() => _compensated.ShouldEqual(1);
}
