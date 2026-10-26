// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_TypeExtensions;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class when_generating_proxies_with_a_slice_location : Specification, IDisposable
{
    const string ExpectedSliceLocation = "static readonly sliceLocation: SliceLocation = new SliceLocation('Scenarios', 'for_ProxyGeneration', 'Generated');";

    JavaScriptRuntime _runtime = null!;
    CommandDescriptor _command = null!;
    string _commandCode = null!;
    string _queryCode = null!;
    string _observableQueryCode = null!;
    bool _commandIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();
        TypeExtensions.SetNamespaceRoots([("Cratis.Arc.ProxyGenerator", "features")]);
    }

    void Because()
    {
        _command = typeof(SimpleCommand).GetMethod(nameof(SimpleCommand.Handle))!
            .ToCommandDescriptor(nameof(SimpleCommand), [], [], "/api/simple", "/output", 0, validationRules: []);
        _commandCode = InMemoryProxyGenerator.GenerateCommand(_command with { SliceLocation = new("Scenarios", "for_ProxyGeneration", "Generated") });
        _commandIsValid = !string.IsNullOrEmpty(_runtime.TranspileTypeScript(_commandCode));

        var query = QueryFor(isObservable: false);
        _queryCode = InMemoryProxyGenerator.GenerateQuery(query);
        _observableQueryCode = InMemoryProxyGenerator.GenerateQuery(query with { IsObservable = true });
    }

    void Destroy() => TypeExtensions.SetNamespaceRoots([]);

    [Fact] void should_resolve_the_command_slice_location_from_its_namespace() =>
        _command.SliceLocation.ShouldEqual(new SliceLocationDescriptor(string.Empty, "Scenarios", "for_ProxyGeneration"));
    [Fact] void should_emit_the_slice_location_on_the_command() => _commandCode.ShouldContain(ExpectedSliceLocation);
    [Fact] void should_import_the_slice_location_type_for_the_command() => _commandCode.ShouldContain("import { PropertyDescriptor, SliceLocation } from '@cratis/arc/reflection';");
    [Fact] void should_generate_valid_typescript_for_the_command() => _commandIsValid.ShouldBeTrue();
    [Fact] void should_emit_the_slice_location_on_the_query() => _queryCode.ShouldContain(ExpectedSliceLocation);
    [Fact] void should_import_the_slice_location_type_for_the_query() => _queryCode.ShouldContain("import { ParameterDescriptor, SliceLocation } from '@cratis/arc/reflection';");
    [Fact] void should_emit_the_slice_location_on_the_observable_query() => _observableQueryCode.ShouldContain(ExpectedSliceLocation);

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }

    static QueryDescriptor QueryFor(bool isObservable) =>
        new(
            typeof(SimpleReadModel),
            typeof(SimpleReadModel).GetMethod("GetAll")!,
            "/api/queries/simple-read-model/get-all",
            "GetAll",
            "SimpleReadModel",
            "SimpleReadModel",
            true,
            isObservable,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            [],
            [],
            [typeof(SimpleReadModel)],
            null,
            [],
            false,
            [])
        {
            SliceLocation = new("Scenarios", "for_ProxyGeneration", "Generated")
        };
}
