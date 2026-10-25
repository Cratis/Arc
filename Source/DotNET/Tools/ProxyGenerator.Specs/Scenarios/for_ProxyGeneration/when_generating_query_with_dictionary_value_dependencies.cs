// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_query_with_dictionary_value_dependencies : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string Compiler = """
        const ts = require(process.argv[1]);
        const path = require('path');
        const files = JSON.parse(require('fs').readFileSync(0, 'utf8'));
        const options = {
            strict: true, noEmit: true, skipLibCheck: true, types: [],
            target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext,
            moduleResolution: ts.ModuleResolutionKind.Bundler
        };
        const host = ts.createCompilerHost(options);
        const readFile = host.readFile.bind(host);
        const fileExists = host.fileExists.bind(host);
        const directoryExists = host.directoryExists.bind(host);
        host.readFile = file => files[path.resolve(file)] ?? readFile(file);
        host.fileExists = file => Object.hasOwn(files, path.resolve(file)) || fileExists(file);
        host.directoryExists = directory => Object.keys(files).some(file => file.startsWith(path.resolve(directory) + path.sep)) || directoryExists(directory);
        const diagnostics = ts.getPreEmitDiagnostics(ts.createProgram(Object.keys(files), options, host));
        for (const diagnostic of diagnostics) {
            console.error(ts.flattenDiagnosticMessageText(diagnostic.messageText, ' '));
        }
        process.exitCode = diagnostics.length === 0 ? 0 : 1;
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    QueryDescriptor _descriptor = null!;
    QueryDescriptor _nullableDescriptor = null!;
    string _generatedCode = string.Empty;
    string _compilerOutput = string.Empty;
    int _compilerExitCode = -1;

    async Task Because()
    {
        var readModel = typeof(DictionaryDependencyReadModel).GetTypeInfo();
        var outputPath = Path.Combine(JavaScriptResources.RepoRoot, ".ai-work", "dictionary-query-compilation");
        var segmentsToSkip = readModel.Namespace!.Split('.').Length;
        var descriptors = readModel.ToQueryDescriptors(outputPath, segmentsToSkip, false, "api", [readModel]).ToArray();
        _descriptor = descriptors.Single(descriptor => descriptor.Name == nameof(DictionaryDependencyReadModel.CountDictionaryValues));
        _nullableDescriptor = descriptors.Single(descriptor => descriptor.Name == nameof(DictionaryDependencyReadModel.CountNullableDictionaryValues));
        var files = GenerateModules(descriptors, outputPath);
        _generatedCode = string.Join('\n', files.Values);
        files[Path.Combine(outputPath, "usage.ts")] = GenerateUsage();
        await Compile(files);
    }

    [Fact] void should_collect_the_enum_value_dependency() => _descriptor.TypesInvolved.ShouldContain(typeof(DictionaryValueStatus));
    [Fact] void should_collect_the_nullable_enum_value_dependency() => _nullableDescriptor.TypesInvolved.ShouldContain(typeof(DictionaryValueStatus));
    [Fact] void should_not_generate_the_nullable_wrapper() => _nullableDescriptor.TypesInvolved.ShouldNotContain(typeof(DictionaryValueStatus?));
    [Fact] void should_not_generate_the_string_concept() => _descriptor.TypesInvolved.ShouldNotContain(typeof(DictionaryValueCode));
    [Fact] void should_not_generate_the_guid_concept() => _descriptor.TypesInvolved.ShouldNotContain(typeof(DictionaryValueIdentifier));
    [Fact] void should_import_the_enum_value() => _generatedCode.ShouldContain($"import {{ DictionaryValueStatus }} from './{GeneratedFileNames.ModuleNameFor(nameof(DictionaryValueStatus))}'");
    [Fact] void should_import_the_guid_value() => _generatedCode.ShouldContain("import { Guid } from '@cratis/fundamentals'");
    [Fact] void should_use_string_for_concept_values() => _generatedCode.ShouldContain("codes: Record<string, string>");
    [Fact] void should_use_guid_for_concept_values() => _generatedCode.ShouldContain("conceptIdentifiers: Record<string, Guid>");
    [Fact] void should_preserve_nullable_number_values() => _generatedCode.ShouldContain("numbers: Record<string, number | null>");
    [Fact] void should_preserve_nullable_enum_values() => _generatedCode.ShouldContain("nullableStatuses: Record<string, DictionaryValueStatus | null>");
    [Fact] void should_preserve_nullable_guid_values() => _generatedCode.ShouldContain("nullableIdentifiers: Record<string, Guid | null>");
    [Fact] void should_semantically_compile_the_generated_modules() => _compilerExitCode.ShouldEqual(0);
    [Fact] void should_not_report_compiler_diagnostics() => _compilerOutput.ShouldBeEmpty();

    static Dictionary<string, string> GenerateModules(QueryDescriptor[] descriptors, string outputPath)
    {
        var files = descriptors.ToDictionary(
            descriptor => Path.Combine(outputPath, GeneratedFileNames.FileNameFor(descriptor.Name)),
            InMemoryProxyGenerator.GenerateQuery);
        files[Path.Combine(outputPath, GeneratedFileNames.FileNameFor(nameof(DictionaryDependencyReadModel)))] =
            InMemoryProxyGenerator.GenerateType(typeof(DictionaryDependencyReadModel).ToTypeDescriptor(outputPath, 0));
        foreach (var type in descriptors.SelectMany(descriptor => descriptor.TypesInvolved).Distinct().Where(type => type.IsEnum))
        {
            files[Path.Combine(outputPath, GeneratedFileNames.FileNameFor(type.Name))] =
                InMemoryProxyGenerator.GenerateEnum(type.ToEnumDescriptor());
        }

        return files;
    }

#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    static string GenerateUsage() => $$"""
        import { Guid } from '@cratis/fundamentals';
        import { CountDictionaryValues, CountDictionaryValuesParameters } from './{{GeneratedFileNames.ModuleNameFor(nameof(DictionaryDependencyReadModel.CountDictionaryValues))}}';
        import { CountNullableDictionaryValues, CountNullableDictionaryValuesParameters } from './{{GeneratedFileNames.ModuleNameFor(nameof(DictionaryDependencyReadModel.CountNullableDictionaryValues))}}';
        import { DictionaryValueStatus } from './{{GeneratedFileNames.ModuleNameFor(nameof(DictionaryValueStatus))}}';

        const parameters: CountDictionaryValuesParameters = {
            statuses: { first: DictionaryValueStatus.active },
            codes: { first: 'item' },
            identifiers: { first: Guid.empty },
            conceptIdentifiers: { first: Guid.empty }
        };
        const nullableParameters: CountNullableDictionaryValuesParameters = {
            numbers: { first: null, second: 42 },
            nullableStatuses: { first: null, second: DictionaryValueStatus.active },
            nullableIdentifiers: { first: null, second: Guid.empty }
        };
        const query = new CountDictionaryValues();
        query.statuses = parameters.statuses;
        query.codes = parameters.codes;
        query.identifiers = parameters.identifiers;
        query.conceptIdentifiers = parameters.conceptIdentifiers;
        const nullableQuery = new CountNullableDictionaryValues();
        nullableQuery.numbers = nullableParameters.numbers;
        nullableQuery.nullableStatuses = nullableParameters.nullableStatuses;
        nullableQuery.nullableIdentifiers = nullableParameters.nullableIdentifiers;
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    async Task Compile(Dictionary<string, string> files)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            WorkingDirectory = JavaScriptResources.RepoRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add(Compiler);
        startInfo.ArgumentList.Add(JavaScriptResources.TypeScriptCompilerPath);
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(files));
        process.StandardInput.Close();
        await process.WaitForExitAsync();
        _compilerExitCode = process.ExitCode;
        _compilerOutput = await output + await error;
    }
}
