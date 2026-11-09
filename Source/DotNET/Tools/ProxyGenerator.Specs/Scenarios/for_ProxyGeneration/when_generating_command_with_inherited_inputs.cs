// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_command_with_inherited_inputs : Specification, IDisposable
{
    JavaScriptRuntime _runtime;
    CommandDescriptor _descriptor;
    string _generatedCode;
    string _serializedBody;
    IReadOnlyList<string> _diagnostics;

    void Establish() => _runtime = new JavaScriptRuntime();

    void Because()
    {
        var type = typeof(CommandWithInheritedInputs).GetTypeInfo();
        _descriptor = type.ToCommandDescriptor("/output", 5, false, "api", [type]);
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_descriptor);
        _diagnostics = _runtime.GetSyntacticDiagnostics(_generatedCode);
        var detailsCode = InMemoryProxyGenerator.GenerateType(typeof(InheritedCommandDetails).ToTypeDescriptor("/output", 5));
        _runtime.Execute(_runtime.TranspileTypeScript(detailsCode));
        _runtime.Execute("globalThis.InheritedCommandDetails = exports.InheritedCommandDetails;");
        _runtime.Execute(_runtime.TranspileTypeScript(_generatedCode));
        _runtime.Execute("globalThis.__fetchHandler = (url, options) => { globalThis.__serializedBody = options.body; };");
        _serializedBody = _runtime.Evaluate<string>(
            "(() => { " +
            "const command = new CommandWithInheritedInputs(); " +
            "command.name = 'Example'; " +
            "command.details = { street: 'Example street' }; " +
            "command.description = 'Description'; " +
            "command.code = 42; " +
            "command.count = 3; " +
            "command.enabled = true; " +
            "command.declaredIgnoredInput = 'Declared'; " +
            "command.ignoredInput = 'Inherited'; " +
            "command.internalInput = 'Excluded'; " +
            "command.execute(); " +
            "return globalThis.__serializedBody; " +
            "})()");
    }

    [Fact] void should_include_the_inherited_scalar_in_the_interface() => _generatedCode.ShouldContain("name?: string;");
    [Fact] void should_include_the_inherited_complex_input_in_the_interface() => _generatedCode.ShouldContain("details?: InheritedCommandDetails;");
    [Fact] void should_include_the_inherited_scalar_accessors() => _generatedCode.ShouldContain("get name(): string");
    [Fact] void should_include_the_inherited_complex_accessors() => _generatedCode.ShouldContain("set details(value: InheritedCommandDetails)");
    [Fact] void should_import_the_inherited_complex_input() => _descriptor.Imports.Any(_ => _.Type == nameof(InheritedCommandDetails)).ShouldBeTrue();
    [Fact] void should_describe_the_inherited_scalar() => _generatedCode.ShouldContain("new PropertyDescriptor('name', String, false)");
    [Fact] void should_describe_the_inherited_complex_input() => _generatedCode.ShouldContain("new PropertyDescriptor('details', InheritedCommandDetails, false)");
    [Fact] void should_include_inputs_from_intermediate_bases() => _generatedCode.ShouldContain("count?: number;");
    [Fact] void should_resolve_hidden_inputs_to_the_most_derived_type() => _descriptor.Properties.Single(_ => _.Name == "Code").Type.ShouldEqual("number");
    [Fact] void should_not_duplicate_overridden_inputs() => _descriptor.Properties.Count(_ => _.Name == "Description").ShouldEqual(1);
    [Fact] void should_not_duplicate_hidden_inputs() => _descriptor.Properties.Count(_ => _.Name == "Code").ShouldEqual(1);
    [Fact] void should_include_declared_json_ignored_inputs() => _generatedCode.ShouldContain("declaredIgnoredInput?: string;");
    [Fact] void should_treat_inherited_json_ignored_inputs_like_declared_inputs() => _descriptor.Properties.Single(_ => _.Name == "IgnoredInput").Type.ShouldEqual(_descriptor.Properties.Single(_ => _.Name == "DeclaredIgnoredInput").Type);
    [Fact] void should_emit_validation_for_the_inherited_property() => _generatedCode.ShouldContain("this.ruleFor(c => c.name).notEmpty();");
    [Fact] void should_have_the_property_referenced_by_validation() => _descriptor.Properties.Any(_ => _.Name == "Name").ShouldBeTrue();
    [Fact] void should_include_inherited_json_ignored_inputs() => _generatedCode.ShouldContain("ignoredInput?: string;");
    [Fact] void should_exclude_non_public_inputs() => _generatedCode.ShouldNotContain("internalInput");
    [Fact] void should_serialize_the_inherited_scalar() => _serializedBody.ShouldContain("\"name\":\"Example\"");
    [Fact] void should_serialize_the_inherited_complex_input() => _serializedBody.ShouldContain("\"details\":{\"street\":\"Example street\"}");
    [Fact] void should_serialize_the_most_derived_hidden_input() => _serializedBody.ShouldContain("\"code\":42");
    [Fact] void should_serialize_inherited_json_ignored_inputs_like_declared_inputs() => _serializedBody.ShouldContain("\"ignoredInput\":\"Inherited\"");
    [Fact] void should_serialize_declared_json_ignored_inputs() => _serializedBody.ShouldContain("\"declaredIgnoredInput\":\"Declared\"");
    [Fact] void should_not_serialize_non_public_inputs() => _serializedBody.ShouldNotContain("internalInput");
    [Fact] void should_produce_valid_typescript() => _diagnostics.ShouldBeEmpty();

    public void Dispose() => _runtime?.Dispose();
}
