// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_selecting_metadata_loaded_queries : Specification
{
    MetadataLoadContext _context;
    MethodInfo[] _selected;
    MethodInfo[] _conventional;
    Type _empty;
    Type _inherited;
    Type _abstract;
    Type _conventionalAbstract;
    QueryDescriptor[] _descriptors;
    QueryDescriptor[] _selfDescriptors;
    Type _value;

    void Establish()
    {
        var assemblyFile = typeof(SelectedReadModel).Assembly.Location;
        var runtimeDirectory = Path.TrimEndingDirectorySeparator(RuntimeEnvironment.GetRuntimeDirectory());
        var runtime = new DirectoryInfo(runtimeDirectory);
        var aspNetCoreDirectory = Path.Combine(runtime.Parent!.Parent!.FullName, "Microsoft.AspNetCore.App", runtime.Name);
        string[] paths =
        [
            .. Directory.GetFiles(runtimeDirectory, "*.dll"),
            .. Directory.GetFiles(aspNetCoreDirectory, "*.dll"),
            .. Directory.GetFiles(Path.GetDirectoryName(assemblyFile)!, "*.dll")
        ];
        _context = new MetadataLoadContext(new PathAssemblyResolver(paths.Distinct(new FileNameComparer())));
    }

    void Because()
    {
        var assembly = _context.LoadFromAssemblyPath(typeof(SelectedReadModel).Assembly.Location);
        _selected = assembly.GetType(typeof(SelectedReadModel).FullName!)!.GetQueryMethods().ToArray();
        _conventional = assembly.GetType(typeof(ConventionalReadModel).FullName!)!.GetQueryMethods().ToArray();
        _empty = assembly.GetType(typeof(NoQueries).FullName!)!;
        _inherited = assembly.GetType(typeof(InheritedSelection).FullName!)!;
        _abstract = assembly.GetType(typeof(ExplicitAbstractReadModel).FullName!)!;
        _conventionalAbstract = assembly.GetType(typeof(ConventionalAbstractReadModel).FullName!)!;
        var selectedType = assembly.GetType(typeof(SelectedReadModel).FullName!)!.GetTypeInfo();
        _descriptors = selectedType.ToQueryDescriptors("output", 0, false, "api", [selectedType]).ToArray();
        var selfType = assembly.GetType(typeof(SelfParameterModel).FullName!)!.GetTypeInfo();
        _selfDescriptors = selfType.ToQueryDescriptors("output", 0, false, "api", [selfType]).ToArray();
        _value = assembly.GetType(typeof(SelfParameterValue).FullName!)!;
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_emit_only_queries_not_self_parameter_helpers() => _selfDescriptors.Select(_ => _.Method.Name).ShouldContainOnly("Collection", "Other", "Service", "All");
    [Fact] void should_normalize_nullable_value_self_parameters_in_metadata() => _value.GetQueryMethods().Select(_ => _.Name).ShouldContainOnly("All");
    [Fact] void should_select_only_marked_queries() => _selected.Select(_ => _.Name).ShouldContainOnly("ById", "Observe");
    [Fact] void should_select_the_marked_overload() => _selected.Single(_ => _.Name == "ById").GetParameters()[0].ParameterType.FullName.ShouldEqual(typeof(string).FullName);
    [Fact] void should_not_emit_task_lifecycle_helpers() => _selected.Any(_ => _.Name.Contains("Replay", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_hide_conventional_siblings() => _conventional.Select(_ => _.Name).ShouldContainOnly("Marked", "Create", "Sibling");
    [Fact] void should_preserve_the_legacy_abstract_owner_exclusion() => _conventionalAbstract.IsQuery().ShouldBeFalse();
    [Fact] void should_expose_marked_queries_on_an_abstract_explicit_owner() => _abstract.IsQuery().ShouldBeTrue();
    [Fact] void should_allow_a_type_with_zero_selected_queries() => _empty.IsQuery().ShouldBeFalse();
    [Fact] void should_emit_only_selected_proxy_descriptors() => _descriptors.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_selected_route() => _descriptors.Single(_ => _.Method.Name == "ById").Route.ShouldEqual("/api/selection/by-id");
    [Fact] void should_preserve_the_selected_http_method() => _descriptors.Single(_ => _.Method.Name == "ById").HttpMethod.ShouldEqual("Query");
    [Fact] void should_preserve_required_argument_information() => _descriptors.Single(_ => _.Method.Name == "ById").RequiredParameters.Single().Name.ShouldEqual("id");
    [Fact] void should_read_an_inherited_setting_from_metadata() => _inherited.GetQueryMethods().Select(_ => _.Name).ShouldContainOnly("All");
}
