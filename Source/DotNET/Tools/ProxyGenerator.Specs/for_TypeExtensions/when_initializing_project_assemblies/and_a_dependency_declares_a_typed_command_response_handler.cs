// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Specs.ActualDependency;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_initializing_project_assemblies;

public class and_a_dependency_declares_a_typed_command_response_handler : Specification
{
    bool _firstInitialization;
    bool _secondInitialization;
    bool _failureInitialization;
    bool _metadataHandledAfterFirstInitialization;
    bool _metadataCollectionHandledAfterFirstInitialization;
    bool _markerOnlyValueHandledAfterFirstInitialization;
    bool _mismatchedAssemblyPresentAfterFirstInitialization;
    bool _dependencyContributesGeneratedTypeDiscoveryProvider;
    bool _dependencyTypePresentAfterSecondInitialization;
    bool _runtimeHandledAfterSecondInitialization;
    bool _runtimePrimitiveCollectionAfterSecondInitialization;
    bool _projectAssembliesEmptyAfterFailure;
    int _rootAssemblyCountBeforeInitialization;
    int _rootAssemblyCountAfterInitializations;
    string _invalidAssemblyFile;
    List<string> _errors;

    void Establish()
    {
        _invalidAssemblyFile = Path.Combine(Path.GetTempPath(), $"proxy-generator-invalid-{Guid.NewGuid():N}.dll");
        _errors = [];
        _rootAssemblyCountBeforeInitialization = CountLoadedRootAssemblies();
        File.WriteAllText(_invalidAssemblyFile, "not a managed assembly");
    }

    void Because()
    {
        _firstInitialization = TypeExtensions.TryInitializeProjectAssemblies(typeof(and_a_dependency_declares_a_typed_command_response_handler).Assembly.Location, _ => { }, _errors.Add);
        using (TypeExtensions.OwnProjectAssemblies())
        {
            var graphAType = FindCurrentMetadataType(typeof(DependencyHandledValue).FullName!);
            _metadataHandledAfterFirstInitialization = graphAType?.IsServerHandledCommandResponseValue() == true;
            _metadataCollectionHandledAfterFirstInitialization = graphAType?.MakeArrayType().IsServerHandledCommandResponseValue() == true;
            _markerOnlyValueHandledAfterFirstInitialization = FindCurrentMetadataType(typeof(MarkerOnlyValue).FullName!)?.IsServerHandledCommandResponseValue() == true;
            _mismatchedAssemblyPresentAfterFirstInitialization = FindCurrentMetadataType(typeof(MismatchedAssemblyMarker).FullName!) is not null;
            _dependencyContributesGeneratedTypeDiscoveryProvider = typeof(DependencyHandledValue).Assembly.GetTypes()
                .Any(_ => _.Name.Contains("GeneratedTypeDiscoveryProvider", StringComparison.Ordinal));
        }

        _secondInitialization = TypeExtensions.TryInitializeProjectAssemblies(typeof(CommandResult).Assembly.Location, _ => { }, _errors.Add);
        using (TypeExtensions.OwnProjectAssemblies())
        {
            _dependencyTypePresentAfterSecondInitialization = FindCurrentMetadataType(typeof(DependencyHandledValue).FullName!) is not null;
            _runtimeHandledAfterSecondInitialization = typeof(DependencyHandledValueHandler).Assembly
                .GetType(typeof(DependencyHandledValueHandler).FullName!)!
                .GetInterfaces()
                .Single(_ => _.IsGenericType &&
                             _.GetGenericTypeDefinition() == typeof(ICommandResponseValueHandler<>) &&
                             _.GetGenericArguments()[0] == typeof(DependencyHandledValue))
                .GetGenericArguments()[0]
                .IsServerHandledCommandResponseValue();
            _runtimePrimitiveCollectionAfterSecondInitialization = typeof(string[]).IsEnumerableOfPrimitiveOrConcept();
        }

        _failureInitialization = TypeExtensions.TryInitializeProjectAssemblies(_invalidAssemblyFile, _ => { }, _errors.Add);
        _projectAssembliesEmptyAfterFailure = !TypeExtensions.Assemblies.Any();

        _rootAssemblyCountAfterInitializations = CountLoadedRootAssemblies();
    }

    void Destroy() => File.Delete(_invalidAssemblyFile);

    static Type? FindCurrentMetadataType(string fullName) =>
        TypeExtensions.Assemblies
            .SelectMany(_ => _.GetTypes())
            .FirstOrDefault(_ => _.FullName == fullName);

    static int CountLoadedRootAssemblies()
    {
        var rootPath = Path.GetFullPath(typeof(and_a_dependency_declares_a_typed_command_response_handler).Assembly.Location);
        return AppDomain.CurrentDomain.GetAssemblies().Count(_ =>
            !_.IsDynamic &&
            !string.IsNullOrEmpty(_.Location) &&
            string.Equals(Path.GetFullPath(_.Location), rootPath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact] void should_initialize_the_real_metadata_graph() => Assert.True(_firstInitialization, string.Join(Environment.NewLine, _errors));
    [Fact] void should_discover_the_dependency_handler() => _metadataHandledAfterFirstInitialization.ShouldBeTrue();
    [Fact] void should_discover_the_dependency_collection_handler() => _metadataCollectionHandledAfterFirstInitialization.ShouldBeTrue();
    [Fact] void should_ignore_a_typed_declaration_without_a_runtime_handler() => _markerOnlyValueHandledAfterFirstInitialization.ShouldBeFalse();
    [Fact] void should_include_a_project_whose_package_id_differs_from_its_assembly_name() => _mismatchedAssemblyPresentAfterFirstInitialization.ShouldBeTrue();
    [Fact] void should_not_pollute_the_host_with_a_generated_type_discovery_provider() => _dependencyContributesGeneratedTypeDiscoveryProvider.ShouldBeFalse();
    [Fact] void should_initialize_a_second_project_graph() => Assert.True(_secondInitialization, string.Join(Environment.NewLine, _errors));
    [Fact] void should_clear_the_first_projects_handler_types() => _dependencyTypePresentAfterSecondInitialization.ShouldBeFalse();
    [Fact] void should_resolve_runtime_handler_contracts_independently_of_the_current_metadata_graph() => _runtimeHandledAfterSecondInitialization.ShouldBeTrue();
    [Fact] void should_resolve_runtime_collection_contracts_independently_of_the_current_metadata_graph() => _runtimePrimitiveCollectionAfterSecondInitialization.ShouldBeTrue();
    [Fact] void should_report_invalid_input_as_a_failed_initialization() => _failureInitialization.ShouldBeFalse();
    [Fact] void should_leave_no_project_assemblies_after_a_failed_initialization() => _projectAssembliesEmptyAfterFailure.ShouldBeTrue();
    [Fact] void should_not_load_another_runtime_copy_of_the_root_assembly() => _rootAssemblyCountAfterInitializations.ShouldEqual(_rootAssemblyCountBeforeInitialization);
}
