// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_command_with_optional_properties : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    CommandDescriptor _descriptor = null!;
    IReadOnlyList<string> _diagnostics = null!;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var commandType = typeof(CommandWithOptionalProperties);
        var method = commandType.GetMethod("Handle") ?? typeof(object).GetMethod("GetHashCode");
        var properties = commandType.GetProperties()
            .Select(p => p.ToPropertyDescriptor())
            .ToList();

        _descriptor = new CommandDescriptor(
            commandType,
            method,
            "/api/commands/command-with-optional-properties",
            "CommandWithOptionalProperties",
            properties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            false,
            ModelDescriptor.Empty,
            [],
            null,
            [],
            false,
            []);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_descriptor);
        _diagnostics = _runtime.GetSyntacticDiagnostics(_generatedCode);
    }

    [Fact] void should_declare_the_optional_backing_field_as_optional() => _generatedCode.ShouldContain("private _description?: string;");
    [Fact] void should_declare_the_optional_getter_as_optional() => _generatedCode.ShouldContain("get description(): string | undefined {");
    [Fact] void should_declare_the_optional_setter_as_optional() => _generatedCode.ShouldContain("set description(value: string | undefined) {");
    [Fact] void should_declare_the_optional_value_type_getter_as_optional() => _generatedCode.ShouldContain("get value(): number | undefined {");
    [Fact] void should_declare_the_optional_enumerable_backing_field_as_optional() => _generatedCode.ShouldContain("private _labels?: string[];");
    [Fact] void should_declare_the_optional_enumerable_getter_as_optional() => _generatedCode.ShouldContain("get labels(): string[] | undefined {");
    [Fact] void should_declare_the_optional_enumerable_setter_as_optional() => _generatedCode.ShouldContain("set labels(value: string[] | undefined) {");
    [Fact] void should_declare_the_required_backing_field_as_definite() => _generatedCode.ShouldContain("private _name!: string;");
    [Fact] void should_declare_the_required_getter_as_required() => _generatedCode.ShouldContain("get name(): string {");
    [Fact] void should_declare_the_required_setter_as_required() => _generatedCode.ShouldContain("set name(value: string) {");
    [Fact] void should_declare_the_required_enumerable_backing_field_as_definite() => _generatedCode.ShouldContain("private _tags!: string[];");
    [Fact] void should_declare_the_required_enumerable_getter_as_required() => _generatedCode.ShouldContain("get tags(): string[] {");
    [Fact] void should_declare_the_required_enumerable_setter_as_required() => _generatedCode.ShouldContain("set tags(value: string[]) {");
    [Fact] void should_declare_the_optional_property_descriptor_as_nullable() => _generatedCode.ShouldContain("new PropertyDescriptor('description', String, true)");
    [Fact] void should_declare_the_required_property_descriptor_as_not_nullable() => _generatedCode.ShouldContain("new PropertyDescriptor('name', String, false)");
    [Fact] void should_produce_parseable_typescript() => _diagnostics.ShouldBeEmpty();

    public void Dispose() => _runtime?.Dispose();
}
