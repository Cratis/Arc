// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Monads;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OneOf;

namespace Cratis.Arc.Generators.Specs.for_CommandOperationGenerator;

public class when_generating_command_result_factories : Specification
{
    string _source;
    Diagnostic[] _diagnostics;
    Assembly _assembly;
    CorrelationId _correlationId;

    void Because()
    {
        var compilation = CSharpCompilation.Create(
            "CommandResultGeneratorSpec",
            [CSharpSyntaxTree.ParseText("""
                using System;
                using System.Collections.Generic;
                using System.Diagnostics.CodeAnalysis;
                using System.Threading.Tasks;
                using Cratis.Arc.Commands.ModelBound;
                using Cratis.Monads;
                using OneOf;
                #pragma warning disable CRATIS001
                namespace Results;
                public record Receipt(string Number);
                public record Created(string Name);
                public record Rejected(string Reason);
                public enum Outcome { Accepted }
                public interface IAnswer { }
                public abstract record Answer;
                public record Fact(string Text);
                [Command] public record ReturnsValueType { public int Handle() => 42; }
                [Command] public record ReturnsTask { public Task<Receipt> Handle() => Task.FromResult(new Receipt("1")); }
                [Command] public record ReturnsNullableValueTask { public ValueTask<Guid?> Handle() => new(Guid.Empty); }
                [Command] public record ReturnsTuple { public (Receipt Receipt, Created Event) Handle() => (new("1"), new("n")); }
                [Command] public record ReturnsOneOf { public OneOf<Outcome, Rejected> Handle() => Outcome.Accepted; }
                [Command] public record ReturnsResult { public Result<Fact, Rejected> Handle() => new Fact("f"); }
                [Command] public record ReturnsCollection { public List<Receipt> Handle() => []; }
                [Command] public record ReturnsNullableReference { public string? Handle() => null; }
                [Command] public record ReturnsNothing { public void Handle() { } }
                [Command] public record ReturnsCompletion { public Task Handle() => Task.CompletedTask; }
                [Command] public record ReturnsInterface { public IAnswer Handle() => null!; }
                [Command] public record ReturnsAbstract { public Answer Handle() => null!; }
                [Command] public record ReturnsObject { public object Handle() => new(); }
                [Command] public record ReturnsPrivate { Hidden Handle() => new(); private record Hidden; }
                [Command] public record ReturnsObsolete { public Legacy Handle() => new(); }
                [Obsolete] public record Legacy;
                [Obsolete] public record Retired;
                [Experimental("CRATIS001")] public record Preview;
                public class Holder<T> { public record Inner; }
                [Command] public record ReturnsObsoleteArgument { public List<Retired> Handle() => []; }
                [Command] public record ReturnsExperimental { public Preview Handle() => new(); }
                [Command] public record ReturnsNestedInObsoleteArgument { public Holder<Retired>.Inner Handle() => new(); }
                [Command] public record ReturnsNestedInOpenGeneric<T> { public Reply Handle() => new(); public record Reply; }
                public class Outer<T> { [Command] public record CommandInOpenGeneric { public Answered Handle() => new(); public record Answered; } }
                [Command] public record ReturnsValueCompletion { public ValueTask Handle() => ValueTask.CompletedTask; }
                public record Alpha;
                public record Beta;
                public record Gamma;
                public record Delta;
                public record Epsilon;
                [Command] public record ReturnsSystemTuple { public Tuple<Alpha, Beta> Handle() => new(new(), new()); }
                [Command] public record ReturnsNestedTuple { public (Gamma, (Delta, Epsilon)) Handle() => (new(), (new(), new())); }
                public record NotACommand { public DateTimeOffset Handle() => default; }
                """)],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(CommandResult).Assembly.Location)
                .Append(typeof(IOneOf).Assembly.Location)
                .Append(typeof(Result<,>).Assembly.Location)
                .Append(typeof(CorrelationId).Assembly.Location)
                .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var driver = CSharpGeneratorDriver.Create(new CommandOperationGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        _source = driver.GetRunResult().Results.Single().GeneratedSources.Single(source => source.HintName == "CommandResults.g.cs").SourceText.ToString();
        var commands = compilation.SyntaxTrees.Single();
        _diagnostics = [.. generatorDiagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning && diagnostic.Location.SourceTree != commands)];
        using var binary = new MemoryStream();
        var emitted = output.Emit(binary);
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        }

        _assembly = Assembly.Load(binary.ToArray());
        RuntimeHelpers.RunModuleConstructor(_assembly.ManifestModule.ModuleHandle);
        _correlationId = CorrelationId.New();
    }

    [Fact] void should_emit_compilable_factories_without_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_at_assembly_load() => _source.ShouldContain("ModuleInitializer");
    [Fact] void should_construct_the_typed_result_directly() => _source.ShouldContain("new global::Cratis.Arc.Commands.CommandResult<global::Results.Receipt>(correlationId, (global::Results.Receipt)response)");
    [Fact] void should_register_each_type_once() => Occurrences("typeof(global::Results.Receipt)").ShouldEqual(1);
    [Fact] void should_register_value_types() => _source.ShouldContain("typeof(int)");
    [Fact] void should_register_the_underlying_type_of_nullable_value_types() => _source.ShouldContain("typeof(global::System.Guid)");
    [Fact] void should_register_tuple_elements() => _source.ShouldContain("typeof(global::Results.Created)");
    [Fact] void should_register_one_of_arms() => _source.ShouldContain("typeof(global::Results.Outcome)");
    [Fact] void should_register_result_arms() => _source.ShouldContain("typeof(global::Results.Fact)");
    [Fact] void should_register_constructed_generic_types() => _source.ShouldContain("typeof(global::System.Collections.Generic.List<global::Results.Receipt>)");
    [Fact] void should_register_nullable_reference_types() => _source.ShouldContain("typeof(string)");
    [Fact] void should_not_register_interfaces() => _source.ShouldNotContain("IAnswer");
    [Fact] void should_not_register_abstract_types() => _source.ShouldNotContain("typeof(global::Results.Answer)");
    [Fact] void should_not_register_object() => _source.ShouldNotContain("typeof(object)");
    [Fact] void should_not_register_wrappers() => _source.ShouldNotContain("OneOf<");
    [Fact] void should_not_register_inaccessible_types() => _source.ShouldNotContain("Hidden");
    [Fact] void should_not_register_obsolete_types() => _source.ShouldNotContain("Legacy");
    [Fact] void should_not_register_obsolete_type_arguments() => _source.ShouldNotContain("Retired");
    [Fact] void should_not_register_experimental_types() => _source.ShouldNotContain("Preview");
    [Fact] void should_not_register_types_nested_in_open_generic_types() => _source.ShouldNotContain("Reply");
    [Fact] void should_not_register_types_nested_in_open_generic_command_owners() => _source.ShouldNotContain("Answered");
    [Fact] void should_not_register_completion_tasks() => _source.ShouldNotContain("typeof(global::System.Threading.Tasks.Task)");
    [Fact] void should_not_register_completion_value_tasks() => _source.ShouldNotContain("typeof(global::System.Threading.Tasks.ValueTask)");
    [Fact] void should_register_system_tuple_elements() => _source.ShouldContain("typeof(global::Results.Alpha)");
    [Fact] void should_not_register_system_tuples_themselves() => _source.ShouldNotContain("System.Tuple<");
    [Fact] void should_register_nested_tuple_elements() => _source.ShouldContain("typeof(global::Results.Epsilon)");
    [Fact] void should_register_nested_tuples_which_can_be_the_response() => _source.ShouldContain("typeof((global::Results.Delta, global::Results.Epsilon))");
    [Fact] void should_not_register_types_of_non_commands() => _source.ShouldNotContain("DateTimeOffset");
    [Fact] void should_wrap_a_reference_type_in_its_exact_result_type() => Wrap("Results.Receipt", "1").GetType().ShouldEqual(typeof(CommandResult<>).MakeGenericType(_assembly.GetType("Results.Receipt")!));
    [Fact] void should_wrap_a_value_type_in_its_exact_result_type() => Wrap(42).ShouldBeOfExactType<CommandResult<int>>();
    [Fact] void should_keep_the_response() => ((CommandResult<int>)Wrap(42)).Response.ShouldEqual(42);
    [Fact] void should_keep_the_correlation_id() => Wrap(42).CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_dispatch_to_the_generated_assembly_not_reflection() => CommandResultFactories.For(_assembly.GetType("Results.Receipt")!)!.Method.DeclaringType!.Assembly.ShouldEqual(_assembly);

    int Occurrences(string text) => _source.Split(text).Length - 1;

    CommandResult Wrap(string typeName, params object[] arguments) => Wrap(Activator.CreateInstance(_assembly.GetType(typeName)!, arguments)!);

    CommandResult Wrap(object response)
    {
        CommandResultFactories.TryCreate(_correlationId, response, out var result).ShouldBeTrue();
        return result!;
    }
}
