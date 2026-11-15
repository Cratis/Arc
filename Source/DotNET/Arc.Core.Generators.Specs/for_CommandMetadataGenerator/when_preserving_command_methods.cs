// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_CommandMetadataGenerator;

public class when_preserving_command_methods : Specification
{
    string _source;
    Diagnostic[] _errors;

    void Because()
    {
        var compilation = CSharpCompilation.Create(
            "CommandMetadataSpec",
            [CSharpSyntaxTree.ParseText("""
                using Cratis.Arc.Commands.ModelBound;
                namespace Commands;
                [Command] public partial record PublicCommand { public void Handle() { } }
                public partial record PublicCommand;
                [Command] internal class PrivateHandler { void Handle() { } }
                public class Container<T> { [Command] private record Nested { void Handle() { } } }
                [Command] file record LocalCommand { public void Handle() { } }
                public record NotACommand { public void Handle() { } }
                """)],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(CommandAttribute).Assembly.Location)
                .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(new CommandMetadataGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        _source = driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString();
        _errors = [.. diagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }

    [Fact] void should_preserve_public_methods() => _source.ShouldContain("DynamicallyAccessedMemberTypes.PublicMethods");
    [Fact] void should_preserve_non_public_methods() => _source.ShouldContain("DynamicallyAccessedMemberTypes.NonPublicMethods");
    [Fact] void should_root_dependencies_at_module_initialization() => _source.ShouldContain("[global::System.Runtime.CompilerServices.ModuleInitializer]");
    [Fact] void should_preserve_record_commands() => _source.ShouldContain("\"Commands.PublicCommand\", \"CommandMetadataSpec\"");
    [Fact] void should_preserve_class_commands() => _source.ShouldContain("\"Commands.PrivateHandler\", \"CommandMetadataSpec\"");
    [Fact] void should_preserve_private_commands_in_generic_types() => _source.ShouldContain("\"Commands.Container`1+Nested\", \"CommandMetadataSpec\"");
    [Fact] void should_preserve_file_local_commands_by_metadata_name() => _source.ShouldContain("__LocalCommand\", \"CommandMetadataSpec\"");
    [Fact] void should_not_preserve_unannotated_types() => _source.ShouldNotContain("NotACommand");
    [Fact] void should_not_duplicate_partial_commands() => _source.Split("\"Commands.PublicCommand\"", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_leave_the_compilation_valid() => _errors.ShouldBeEmpty();
}
