// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TemplateTypes;

public class when_generating_lint_compliant_output : Specification
{
    List<string> _contents = [];
    string _directory = null!;
    string _output = null!;
    int _exitCode;

    async Task Because()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        var pending = new Dictionary<string, (string Content, string SourceTypeName)>();
        foreach (var enumerable in new[] { false, true })
        {
            foreach (var populated in new[] { false, true })
            {
                foreach (var validated in new[] { false, true })
                {
                    var descriptor = new
                    {
                        Name = "Sample",
                        Route = "api/sample",
                        Type = typeof(string),
                        Method = typeof(string).GetMethod(nameof(string.Clone)),
                        Model = "string",
                        Constructor = "String",
                        IsEnumerable = enumerable,
                        HasResponse = enumerable,
                        ResponseType = new { Name = "number", Constructor = "Number", IsEnumerable = false },
                        Properties = populated ? new[] { new { Name = "Name", Type = "string", Constructor = "String", IsNullable = false } } : [],
                        Parameters = populated ? new[] { new { Name = "Name", Type = "string", Constructor = "String", IsEnumerable = false, IsOptional = false } } : [],
                        RequiredParameters = Array.Empty<object>(),
                        Imports = Array.Empty<object>(),
                        HasValidationRules = validated && populated,
                        ValidationRules = populated ? new[] { new { PropertyName = "Name", Rules = new[] { new { RuleName = "notEmpty", Arguments = Array.Empty<object>(), HasStaticSeverity = true } } } } : [],
                        Roles = Array.Empty<string>(),
                        TreatWarningsAsErrors = false,
                        TreatWarningsAsErrorsForPolicy = false,
                        HttpMethod = "Post"
                    };
                    Add(TemplateTypes.Command(descriptor));
                    Add(TemplateTypes.Query(descriptor));
                    Add(TemplateTypes.ObservableQuery(descriptor));
                    Add(TemplateTypes.Type(descriptor));
                    Add(TemplateTypes.Interface(descriptor));
                }
            }
        }
        Add(TemplateTypes.Enum(new { Name = "Sample", Values = new[] { new { Name = "First", Value = 1 } } }));
        Add(TemplateTypes.FlagsEnum(new { Name = "Sample", Values = new[] { new { Name = "First", Value = 1 } }, AllFlagsExpression = "Sample.first" }));
        Add(TypeScriptContentCombiner.Combine([TemplateTypes.Type(new { Name = "First", Properties = Array.Empty<object>(), Imports = Array.Empty<object>() }), TemplateTypes.Interface(new { Name = "Second", Properties = Array.Empty<object>(), Imports = Array.Empty<object>() })]));
        Add(ResponseHooks());
        await DescriptorExtensions.FlushPendingContent(pending, new Dictionary<string, GeneratedFileMetadata>(), _ => { });
        _contents = [.. pending.Keys.Select(File.ReadAllText)];

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "eslint.config.mjs")))
        {
            root = root.Parent;
        }
        root.ShouldNotBeNull();
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = root!.FullName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("Source/DotNET/Tools/ProxyGenerator.Specs/for_TemplateTypes/generated-output-contract.mjs");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(_contents));
            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // Node exited before reading its input; its own diagnostics below explain why.
        }
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        await process.WaitForExitAsync(deadline.Token);
        _output = await stdout + await stderr;
        _exitCode = process.ExitCode;
        Console.WriteLine(_output);

        void Add(string content) => pending.Add(Path.Combine(_directory, $"Fixture{pending.Count}.ts"), (content, "Sample"));
    }

    [Fact] void should_lint_every_generated_shape_without_warnings() => Assert.True(_exitCode == 0, _output);
    [Fact] void should_run_actual_eslint_nine_in_all_three_profiles() => _output.Split('\n').Count(line => line.StartsWith("actual9/", StringComparison.Ordinal) && line.Contains("actual ESLint 9.", StringComparison.Ordinal) && line.Contains("directly linted 44 marked generated fixtures", StringComparison.Ordinal)).ShouldEqual(3);
    [Fact] void should_exercise_a_nonempty_population() => _contents.Count.ShouldEqual(44);
    [Fact] void should_not_emit_eslint_directives() => _contents.ShouldContainOnly(_contents.Where(content => !content.Contains("eslint-disable", StringComparison.Ordinal)));
    [Fact] void should_not_emit_typescript_suppressions() => _contents.ShouldContainOnly(_contents.Where(content => !content.Contains("@ts-ignore", StringComparison.Ordinal)));
    [Fact] void should_supply_the_command_response_generic() => _contents.ShouldContain(content => content.Contains("useCommand<Sample, ISample, number>", StringComparison.Ordinal));
    [Fact] void should_preserve_empty_interfaces() => _contents.ShouldContain(content => content.Contains("export interface ISample {", StringComparison.Ordinal));
    [Fact] void should_preserve_metadata_recognition() => Directory.GetFiles(_directory).All(file => GeneratedFileMetadata.IsGeneratedFile(file, out _)).ShouldBeTrue();

    static string ResponseHooks()
    {
        var contents = new List<string>
        {
            TemplateTypes.Type(new { Name = "Response", Properties = Array.Empty<object>(), Imports = Array.Empty<object>() }),
            TemplateTypes.Enum(new { Name = "Choice", Values = new[] { new { Name = "First", Value = 1 } } }),
            TemplateTypes.Interface(new { Name = "IBase", Properties = new[] { new { Name = "Name", Type = "string", IsNullable = false } }, Imports = Array.Empty<object>() }),
            TemplateTypes.Interface(new { Name = "IInherited", BaseTypeName = "IBase", Properties = Array.Empty<object>(), Imports = Array.Empty<object>() })
        };
        foreach (var (name, response, constructor, enumerable) in new[]
        {
            ("NumberResponse", "number", "Number", false),
            ("CustomResponse", "Response", "Response", false),
            ("EnumResponse", "Choice", "Number", false),
            ("ArrayResponse", "Response[]", "Response", true),
            ("VoidResponse", "void", "Object", false)
        })
        {
            contents.Add(TemplateTypes.Command(new
            {
                Name = name,
                Route = "api/response",
                HasResponse = response != "void",
                ResponseType = new { Name = response, Constructor = constructor, IsEnumerable = enumerable },
                Properties = Array.Empty<object>(),
                Parameters = Array.Empty<object>(),
                Imports = Array.Empty<object>(),
                Roles = Array.Empty<string>(),
                TreatWarningsAsErrorsForPolicy = false
            }));
        }
        contents.Add(string.Join<string>('\n',
        [
            "// Existing public empty interfaces remain augmentable and accept non-nullish primitives.",
            "export interface INumberResponse { route?: string; }",
            "export const augmented: INumberResponse = { route: 'supported' };",
            "export const empty: ICustomResponse = 42;",
            "export interface IInherited { additional?: string; }",
            "export const inherited: IInherited = { name: 'base', additional: 'merged' };",
            "export const numberHook = NumberResponse.use();",
            "export const customHook = CustomResponse.use();",
            "export const enumHook = EnumResponse.use();",
            "export const arrayHook = ArrayResponse.use();",
            "export const voidHook = VoidResponse.use();"
        ]));
        return TypeScriptContentCombiner.Combine(contents);
    }

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
