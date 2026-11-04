// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TemplateTypes;

public class when_generating_framework_neutral_proxies : Specification
{
    readonly List<string> _contents = [];
    readonly List<bool> _reactMatchesDefault = [];
    string _output = string.Empty;
    int _exitCode;

    async Task Because()
    {
        var variants = from enumerable in new[] { false, true }
                       from populated in new[] { false, true }
                       from validated in new[] { false, true }
                       select (enumerable, populated, validated);
        foreach (var (enumerable, populated, validated) in variants)
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
                HttpMethod = "Query",
                IsFrameworkNeutral = true
            };
            IDictionary<string, object?> implicitReact = new ExpandoObject();
            foreach (var property in descriptor.GetType().GetProperties().Where(property => property.Name != nameof(descriptor.IsFrameworkNeutral)))
            {
                implicitReact[property.Name] = property.GetValue(descriptor);
            }
            IDictionary<string, object?> explicitReact = new ExpandoObject();
            foreach (var property in implicitReact) explicitReact[property.Key] = property.Value;
            explicitReact[nameof(descriptor.IsFrameworkNeutral)] = false;
            foreach (var template in new[] { TemplateTypes.Command, TemplateTypes.Query, TemplateTypes.ObservableQuery })
            {
                _contents.Add(template(descriptor));
                _reactMatchesDefault.Add(template(implicitReact) == template(explicitReact));
            }
        }

        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = JavaScriptResources.RepoRoot,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("Source/DotNET/Tools/ProxyGenerator.Specs/for_TemplateTypes/framework-neutral-contract.mjs");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(_contents));
        process.StandardInput.Close();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await process.WaitForExitAsync(deadline.Token);
        _output = await stdout + await stderr;
        _exitCode = process.ExitCode;
    }

    [Fact] void should_not_import_react() => _contents.ShouldNotContain(content => content.Contains("react", StringComparison.OrdinalIgnoreCase));
    [Fact] void should_not_emit_hooks() => _contents.ShouldNotContain(content => content.Contains("static use", StringComparison.Ordinal));
    [Fact] void should_preserve_default_react_output_byte_for_byte() => _reactMatchesDefault.TrueForAll(value => value).ShouldBeTrue();
    [Fact] void should_compile_imperative_calls_without_a_react_adapter() => Assert.True(_exitCode == 0, _output);
}
