// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_with_explicit_metadata_registration_from_the_command_line : Specification, IDisposable
{
    const string CircleIdentifier = "9b8f7ef8-b16d-4e2c-bec9-20930f04f687";
    const string RectangleIdentifier = "60c89a76-2848-4513-8cf1-d89ee32f1953";

    JavaScriptRuntime _runtime = null!;
    string _outputPath = null!;
    string _generatedDrawing = string.Empty;
    string _registeredFieldName = string.Empty;
    string _registeredDerivativeNames = string.Empty;
    string _deserializedShapeNames = string.Empty;
    string _deserializedShapeValues = string.Empty;
    int _exitCode = -1;
    bool _registeredFieldIsEnumerable;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();
        _outputPath = Path.Combine(Path.GetTempPath(), $"arc-explicit-metadata-{Guid.NewGuid():N}");
    }

    async Task Because()
    {
        var generatorAssembly = typeof(Generator).Assembly.Location;
        var specificationsAssembly = typeof(GeneratedMetadataDrawing).Assembly.Location;
        var typesToGenerate = new HashSet<Type>
        {
            typeof(GeneratedMetadataDrawing),
            typeof(IGeneratedMetadataShape),
            typeof(GeneratedMetadataCircle),
            typeof(GeneratedMetadataRectangle)
        };
        var excludedTypeNames = typeof(GeneratedMetadataDrawing).Assembly.GetTypes()
            .Where(type => !typesToGenerate.Contains(type) && type.FullName is not null)
            .Select(type => type.FullName!)
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
        startInfo.ArgumentList.Add("--use-explicit-metadata-registration");
        foreach (var typeName in excludedTypeNames)
        {
            startInfo.ArgumentList.Add($"--exclude-type={typeName}");
        }

        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(standardOutput, standardError);
        _exitCode = process.ExitCode;
        if (_exitCode != 0)
        {
            return;
        }

        var drawing = ReadGeneratedType(nameof(GeneratedMetadataDrawing));
        var shape = ReadGeneratedType(nameof(IGeneratedMetadataShape));
        var circle = ReadGeneratedType(nameof(GeneratedMetadataCircle));
        var rectangle = ReadGeneratedType(nameof(GeneratedMetadataRectangle));
        _generatedDrawing = drawing;

        var combined = TypeScriptContentCombiner.Combine([drawing, shape, circle, rectangle]);
        var javaScript = _runtime.TranspileTypeScript(combined, experimentalDecorators: false);
        _runtime.Execute(javaScript);
        _runtime.Execute(
            $"globalThis.{nameof(GeneratedMetadataDrawing)} = exports.{nameof(GeneratedMetadataDrawing)};" +
            $"globalThis.{nameof(GeneratedMetadataCircle)} = exports.{nameof(GeneratedMetadataCircle)};" +
            $"globalThis.{nameof(GeneratedMetadataRectangle)} = exports.{nameof(GeneratedMetadataRectangle)};");

        _registeredFieldName = _runtime.Evaluate<string>(
            $"Fields.getFieldsForType(globalThis.{nameof(GeneratedMetadataDrawing)})[0].name")!;
        _registeredFieldIsEnumerable = _runtime.Evaluate<bool>(
            $"Fields.getFieldsForType(globalThis.{nameof(GeneratedMetadataDrawing)})[0].enumerable");
        _registeredDerivativeNames = _runtime.Evaluate<string>(
            $"Fields.getFieldsForType(globalThis.{nameof(GeneratedMetadataDrawing)})[0].derivatives.map(type => type.name).join(',')")!;

        _runtime.Execute(
            $"globalThis.__generatedMetadataDrawing = JsonSerializer.deserializeFromInstance(globalThis.{nameof(GeneratedMetadataDrawing)}, {{" +
            $"shapes: [{{ _derivedTypeId: '{CircleIdentifier}', name: 'Circle', radius: 3 }}, " +
            $"{{ _derivedTypeId: '{RectangleIdentifier}', name: 'Rectangle', width: 4 }}] }});");
        _deserializedShapeNames = _runtime.Evaluate<string>(
            "globalThis.__generatedMetadataDrawing.shapes.map(shape => shape.constructor.name).join(',')")!;
        _deserializedShapeValues = _runtime.Evaluate<string>(
            "`${globalThis.__generatedMetadataDrawing.shapes[0].name}:${globalThis.__generatedMetadataDrawing.shapes[0].radius}," +
            "${globalThis.__generatedMetadataDrawing.shapes[1].name}:${globalThis.__generatedMetadataDrawing.shapes[1].width}`")!;
    }

    [Fact] void should_run_the_generator_successfully() => _exitCode.ShouldEqual(0);
    [Fact] void should_emit_imperative_metadata_registration() => _generatedDrawing.ShouldContain($"field({nameof(IGeneratedMetadataShape)}, true, [{nameof(GeneratedMetadataCircle)}, {nameof(GeneratedMetadataRectangle)}])");
    [Fact] void should_not_emit_decorator_syntax() => _generatedDrawing.ShouldNotContain("@field(");
    [Fact] void should_register_the_collection_field() => _registeredFieldName.ShouldEqual("shapes");
    [Fact] void should_register_the_field_as_enumerable() => _registeredFieldIsEnumerable.ShouldBeTrue();
    [Fact] void should_register_the_interface_derivatives() => _registeredDerivativeNames.ShouldEqual($"{nameof(GeneratedMetadataCircle)},{nameof(GeneratedMetadataRectangle)}");
    [Fact] void should_deserialize_each_collection_item_to_its_derived_type() => _deserializedShapeNames.ShouldEqual($"{nameof(GeneratedMetadataCircle)},{nameof(GeneratedMetadataRectangle)}");
    [Fact] void should_deserialize_the_derived_type_fields() => _deserializedShapeValues.ShouldEqual("Circle:3,Rectangle:4");

    public void Dispose()
    {
        _runtime?.Dispose();
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, true);
        }

        GC.SuppressFinalize(this);
    }

    string ReadGeneratedType(string typeName)
    {
        var path = Directory.GetFiles(_outputPath, $"{typeName}.ts", SearchOption.AllDirectories).Single();
        var content = File.ReadAllText(path);
        var metadataLineEnd = content.IndexOf('\n');
        return metadataLineEnd < 0 ? content : content[(metadataLineEnd + 1)..];
    }
}
