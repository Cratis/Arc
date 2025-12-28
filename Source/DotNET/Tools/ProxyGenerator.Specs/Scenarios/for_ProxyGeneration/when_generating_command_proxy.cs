// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_command_proxy : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    CommandDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var commandType = typeof(SimpleCommand);
        var properties = commandType.GetProperties()
            .Select(p => p.ToPropertyDescriptor())
            .ToList();

        _descriptor = new CommandDescriptor(
            commandType,
            commandType.GetMethod("Handle"),
            "/api/commands/simple-command",
            "SimpleCommand",
            properties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            false,
            ModelDescriptor.Empty,
            [],
            null,
            []);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_descriptor);

        try
        {
            var transpiledCode = _runtime.TranspileTypeScript(_generatedCode);
            _typeScriptIsValid = !string.IsNullOrEmpty(transpiledCode);
        }
        catch
        {
            _typeScriptIsValid = false;
        }
    }

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_contain_class_name() => _generatedCode.ShouldContain("class SimpleCommand");
    [Fact] void should_contain_route() => _generatedCode.ShouldContain("/api/commands/simple-command");
    [Fact] void should_contain_name_property() => _generatedCode.ShouldContain("name");
    [Fact] void should_contain_value_property() => _generatedCode.ShouldContain("value");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
