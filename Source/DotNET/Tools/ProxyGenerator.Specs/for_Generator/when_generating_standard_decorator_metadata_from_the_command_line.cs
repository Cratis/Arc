// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_standard_decorator_metadata_from_the_command_line : Specification, IDisposable
{
    const string CircleIdentifier = "9b8f7ef8-b16d-4e2c-bec9-20930f04f687";
    const string RectangleIdentifier = "60c89a76-2848-4513-8cf1-d89ee32f1953";

    JavaScriptRuntime _runtime = null!;
    string _temporaryPath = null!;
    string _outputPath = null!;
    string _generatedCircle = string.Empty;
    string _generatedDrawing = string.Empty;
    string _transpiledJavaScript = string.Empty;
    string _metadataFieldNames = string.Empty;
    string _registeredDerivativeNames = string.Empty;
    string _deserializedDrawingType = string.Empty;
    string _deserializedShapeNames = string.Empty;
    string _deserializedShapeValues = string.Empty;
    string _deserializedDates = string.Empty;
    string _derivedTypeIdentifiers = string.Empty;
    string _standardCompilerOutput = string.Empty;
    string _legacyCompilerOutput = string.Empty;
    int _generatorExitCode = -1;
    int _standardCompilerExitCode = -1;
    int _legacyCompilerExitCode = -1;
    bool _circleExtendsBase;
    bool _deserializedDatesAreDateInstances;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();
        _temporaryPath = Path.Combine(Path.GetTempPath(), $"arc-standard-decorator-metadata-{Guid.NewGuid():N}");
        _outputPath = Path.Combine(_temporaryPath, "generated");
        Directory.CreateDirectory(_outputPath);
        CopyDirectory(
            Path.Combine(JavaScriptResources.NodeModulesRoot, "node_modules", "@cratis", "fundamentals"),
            Path.Combine(_temporaryPath, "node_modules", "@cratis", "fundamentals"));
    }

    async Task Because()
    {
        _generatorExitCode = await RunGenerator();
        if (_generatorExitCode != 0)
        {
            return;
        }

        _generatedDrawing = ReadGeneratedType(nameof(GeneratedMetadataDrawing));
        var generatedShape = ReadGeneratedType(nameof(IGeneratedMetadataShape));
        var generatedBase = ReadGeneratedType(nameof(GeneratedMetadataShapeBase));
        _generatedCircle = ReadGeneratedType(nameof(GeneratedMetadataCircle));
        var generatedRectangle = ReadGeneratedType(nameof(GeneratedMetadataRectangle));

        var standardCompilation = await CompileGeneratedTypes("standard", experimentalDecorators: false);
        _standardCompilerExitCode = standardCompilation.ExitCode;
        _standardCompilerOutput = standardCompilation.Output;

        var legacyCompilation = await CompileGeneratedTypes("legacy", experimentalDecorators: true);
        _legacyCompilerExitCode = legacyCompilation.ExitCode;
        _legacyCompilerOutput = legacyCompilation.Output;

        if (_standardCompilerExitCode != 0 || _legacyCompilerExitCode != 0)
        {
            return;
        }

        var combined = TypeScriptContentCombiner.Combine([_generatedDrawing, generatedShape, generatedBase, _generatedCircle, generatedRectangle]);
        _transpiledJavaScript = _runtime.TranspileTypeScript(combined, experimentalDecorators: false);
        _runtime.Execute(_transpiledJavaScript);
        _runtime.Execute(
            $"globalThis.{nameof(GeneratedMetadataDrawing)} = exports.{nameof(GeneratedMetadataDrawing)};" +
            $"globalThis.{nameof(GeneratedMetadataShapeBase)} = exports.{nameof(GeneratedMetadataShapeBase)};" +
            $"globalThis.{nameof(GeneratedMetadataCircle)} = exports.{nameof(GeneratedMetadataCircle)};" +
            $"globalThis.{nameof(GeneratedMetadataRectangle)} = exports.{nameof(GeneratedMetadataRectangle)};");

        _metadataFieldNames = _runtime.Evaluate<string>(
            $"Fields.getFieldsForType(globalThis.{nameof(GeneratedMetadataCircle)}).map(field => field.name).join(',')")!;
        _registeredDerivativeNames = _runtime.Evaluate<string>(
            $"Fields.getFieldsForType(globalThis.{nameof(GeneratedMetadataDrawing)})[0].derivatives.map(type => type.name).join(',')")!;
        _circleExtendsBase = _runtime.Evaluate<bool>(
            $"globalThis.{nameof(GeneratedMetadataShapeBase)}.prototype.isPrototypeOf(globalThis.{nameof(GeneratedMetadataCircle)}.prototype)");
        _derivedTypeIdentifiers = _runtime.Evaluate<string>(
            $"`${{require('@cratis/fundamentals').DerivedType.get(globalThis.{nameof(GeneratedMetadataCircle)})}},${{require('@cratis/fundamentals').DerivedType.get(globalThis.{nameof(GeneratedMetadataRectangle)})}}`")!;

        _runtime.Execute(
            $"globalThis.__generatedMetadataDrawing = JsonSerializer.deserializeFromInstance(globalThis.{nameof(GeneratedMetadataDrawing)}, {{" +
            $"shapes: [{{ _derivedTypeId: '{CircleIdentifier}', createdAt: '2026-08-16T09:30:00.000Z', name: 'Circle', radius: 3 }}, " +
            $"{{ _derivedTypeId: '{RectangleIdentifier}', createdAt: '2026-08-17T10:45:00.000Z', name: 'Rectangle', width: 4 }}] }});");
        _deserializedDrawingType = _runtime.Evaluate<string>("globalThis.__generatedMetadataDrawing.constructor.name")!;
        _deserializedShapeNames = _runtime.Evaluate<string>(
            "globalThis.__generatedMetadataDrawing.shapes.map(shape => shape.constructor.name).join(',')")!;
        _deserializedShapeValues = _runtime.Evaluate<string>(
            "`${globalThis.__generatedMetadataDrawing.shapes[0].name}:${globalThis.__generatedMetadataDrawing.shapes[0].radius}," +
            "${globalThis.__generatedMetadataDrawing.shapes[1].name}:${globalThis.__generatedMetadataDrawing.shapes[1].width}`")!;
        _deserializedDatesAreDateInstances = _runtime.Evaluate<bool>(
            "globalThis.__generatedMetadataDrawing.shapes.every(shape => shape.createdAt instanceof Date)");
        _deserializedDates = _runtime.Evaluate<string>(
            "globalThis.__generatedMetadataDrawing.shapes.map(shape => shape.createdAt.toISOString()).join(',')")!;
    }

    [Fact] void should_run_the_generator_successfully() => _generatorExitCode.ShouldEqual(0);
    [Fact] void should_emit_the_field_decorator() => _generatedDrawing.ShouldContain($"@field({nameof(IGeneratedMetadataShape)}, true, [{nameof(GeneratedMetadataCircle)}, {nameof(GeneratedMetadataRectangle)}])");
    [Fact] void should_emit_the_derived_type_decorator() => _generatedCircle.ShouldContain($"@derivedType('{CircleIdentifier}')");
    [Fact] void should_emit_the_generated_base_class() => _generatedCircle.ShouldContain($"extends {nameof(GeneratedMetadataShapeBase)}");
    [Fact] void should_not_emit_imperative_metadata_registration() => _generatedDrawing.ShouldNotContain(".prototype");
    [Fact] void should_semantically_compile_with_standard_decorators() => _standardCompilerExitCode.ShouldEqual(0);
    [Fact] void should_not_report_standard_compiler_diagnostics() => _standardCompilerOutput.ShouldBeEmpty();
    [Fact] void should_semantically_compile_with_legacy_decorators() => _legacyCompilerExitCode.ShouldEqual(0);
    [Fact] void should_not_report_legacy_compiler_diagnostics() => _legacyCompilerOutput.ShouldBeEmpty();
    [Fact] void should_execute_the_standard_decorator_helpers() => _transpiledJavaScript.ShouldContain("__esDecorate");
    [Fact] void should_expose_inherited_and_declared_metadata_before_construction() => _metadataFieldNames.ShouldEqual("createdAt,name,radius");
    [Fact] void should_register_the_interface_derivatives() => _registeredDerivativeNames.ShouldEqual($"{nameof(GeneratedMetadataCircle)},{nameof(GeneratedMetadataRectangle)}");
    [Fact] void should_preserve_the_generated_inheritance_chain() => _circleExtendsBase.ShouldBeTrue();
    [Fact] void should_register_both_derived_type_identifiers() => _derivedTypeIdentifiers.ShouldEqual($"{CircleIdentifier},{RectangleIdentifier}");
    [Fact] void should_type_the_first_deserialization_as_the_generated_drawing() => _deserializedDrawingType.ShouldEqual(nameof(GeneratedMetadataDrawing));
    [Fact] void should_deserialize_each_collection_item_to_its_derived_type() => _deserializedShapeNames.ShouldEqual($"{nameof(GeneratedMetadataCircle)},{nameof(GeneratedMetadataRectangle)}");
    [Fact] void should_deserialize_the_inherited_dates_as_date_instances() => _deserializedDatesAreDateInstances.ShouldBeTrue();
    [Fact] void should_deserialize_the_inherited_date_values() => _deserializedDates.ShouldEqual("2026-08-16T09:30:00.000Z,2026-08-17T10:45:00.000Z");
    [Fact] void should_deserialize_the_inherited_and_derived_values() => _deserializedShapeValues.ShouldEqual("Circle:3,Rectangle:4");

    public void Dispose()
    {
        _runtime?.Dispose();
        if (Directory.Exists(_temporaryPath))
        {
            Directory.Delete(_temporaryPath, true);
        }

        GC.SuppressFinalize(this);
    }

    async Task<int> RunGenerator()
    {
        var generatorAssembly = typeof(Generator).Assembly.Location;
        var specificationsAssembly = typeof(GeneratedMetadataDrawing).Assembly.Location;
        var typesToGenerate = new HashSet<Type>
        {
            typeof(GeneratedMetadataDrawing),
            typeof(IGeneratedMetadataShape),
            typeof(GeneratedMetadataShapeBase),
            typeof(GeneratedMetadataCircle),
            typeof(GeneratedMetadataRectangle)
        };
        var excludedTypeNames = typeof(GeneratedMetadataDrawing).Assembly.GetTypes()
            .Where(type => !typesToGenerate.Contains(type) && type.FullName is not null)
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal);

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(generatorAssembly);
        startInfo.ArgumentList.Add(specificationsAssembly);
        startInfo.ArgumentList.Add(_outputPath);
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("--library-mode");
        startInfo.ArgumentList.Add("--skip-index-generation");
        foreach (var typeName in excludedTypeNames)
        {
            startInfo.ArgumentList.Add($"--exclude-type={typeName}");
        }

        var result = await RunProcess(startInfo);
        return result.ExitCode;
    }

    async Task<ProcessResult> CompileGeneratedTypes(string configurationName, bool experimentalDecorators)
    {
        var configurationPath = Path.Combine(_temporaryPath, $"tsconfig.{configurationName}.json");
        var configuration = new
        {
            compilerOptions = new
            {
                target = "ES2022",
                module = "ES2022",
                moduleResolution = "Bundler",
                experimentalDecorators,
                emitDecoratorMetadata = experimentalDecorators,
                strict = true,
                skipLibCheck = false,
                types = Array.Empty<string>(),
                noEmit = true
            },
            include = new[] { "generated/**/*.ts" }
        };
        await File.WriteAllTextAsync(configurationPath, JsonSerializer.Serialize(configuration));

        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = _temporaryPath
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add(
            "const ts = require(process.argv[1]);" +
            "const configPath = process.argv[2];" +
            "const configFile = ts.readConfigFile(configPath, ts.sys.readFile);" +
            "if (configFile.error) { console.error(ts.flattenDiagnosticMessageText(configFile.error.messageText, ' ')); process.exit(1); }" +
            "const parsed = ts.parseJsonConfigFileContent(configFile.config, ts.sys, require('path').dirname(configPath));" +
            "const diagnostics = [...parsed.errors, ...ts.getPreEmitDiagnostics(ts.createProgram(parsed.fileNames, parsed.options))];" +
            "for (const diagnostic of diagnostics) { console.error(ts.flattenDiagnosticMessageText(diagnostic.messageText, ' ')); }" +
            "process.exit(diagnostics.length === 0 ? 0 : 1);");
        startInfo.ArgumentList.Add(JavaScriptResources.TypeScriptCompilerPath);
        startInfo.ArgumentList.Add(configurationPath);
        return await RunProcess(startInfo);
    }

    static async Task<ProcessResult> RunProcess(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(standardOutput, standardError);
        return new(process.ExitCode, string.Concat(await standardOutput, await standardError).Trim());
    }

    string ReadGeneratedType(string typeName)
    {
        var path = Directory.GetFiles(_outputPath, $"{typeName}.ts", SearchOption.AllDirectories).Single();
        var content = File.ReadAllText(path);
        var metadataLineEnd = content.IndexOf('\n');
        return metadataLineEnd < 0 ? content : content[(metadataLineEnd + 1)..];
    }

    static void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);
        foreach (var file in Directory.GetFiles(sourcePath))
        {
            File.Copy(file, Path.Combine(destinationPath, Path.GetFileName(file)), true);
        }

        foreach (var directory in Directory.GetDirectories(sourcePath))
        {
            CopyDirectory(directory, Path.Combine(destinationPath, Path.GetFileName(directory)));
        }
    }

    sealed record ProcessResult(int ExitCode, string Output);
}
