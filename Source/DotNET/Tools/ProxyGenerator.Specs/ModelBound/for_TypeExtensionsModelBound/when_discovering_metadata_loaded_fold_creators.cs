// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_discovering_metadata_loaded_fold_creators : Specification
{
    MetadataLoadContext _context;
    MetadataLoadContext _unresolvableContext;
    MethodInfo[] _methods;
    MethodInfo[] _methodsWithUnresolvableMetadata;
    Type _foldOnly;

    void Establish()
    {
        var assemblyFile = typeof(FoldReadModel).Assembly.Location;
        var runtimeDirectory = Path.GetDirectoryName(RuntimeEnvironment.GetRuntimeDirectory())!;
        var version = Path.GetFileName(runtimeDirectory);
        var shared = Directory.GetParent(Directory.GetParent(runtimeDirectory)!.FullName)!;
        var aspNetCoreDirectory = Path.Combine(shared.FullName, "Microsoft.AspNetCore.App", version);
        string[] paths =
        [
            .. Directory.GetFiles(runtimeDirectory, "*.dll"),
            .. Directory.GetFiles(aspNetCoreDirectory, "*.dll"),
            .. Directory.GetFiles(Path.GetDirectoryName(assemblyFile)!, "*.dll")
        ];
        _context = new MetadataLoadContext(new PathAssemblyResolver(paths.Distinct(new FileNameComparer())));
        _unresolvableContext = new MetadataLoadContext(new PathAssemblyResolver(paths
            .Where(_ => Path.GetFileName(_) != Path.GetFileName(typeof(OrderId).BaseType!.Assembly.Location))
            .Distinct(new FileNameComparer())));
    }

    void Because()
    {
        var assembly = _context.LoadFromAssemblyPath(typeof(FoldReadModel).Assembly.Location);
        _methods = assembly.GetType(typeof(FoldReadModel).FullName!)!.GetQueryMethods().ToArray();
        _foldOnly = assembly.GetType(typeof(FoldOnlyReadModel).FullName!)!;
        var unresolvedAssembly = _unresolvableContext.LoadFromAssemblyPath(typeof(FoldReadModel).Assembly.Location);
        _methodsWithUnresolvableMetadata = unresolvedAssembly.GetType(typeof(FoldReadModel).FullName!)!.GetQueryMethods().ToArray();
    }

    void Destroy()
    {
        _context.Dispose();
        _unresolvableContext.Dispose();
    }

    [Fact] void should_keep_only_the_five_real_queries() => _methods.Length.ShouldEqual(5);
    [Fact] void should_keep_the_concept_create_query() => HasCreate(typeof(OrderId).FullName!).ShouldBeTrue();
    [Fact] void should_keep_the_unmarked_class_create_query() => HasCreate(typeof(Unmarked).FullName!).ShouldBeTrue();
    [Fact] void should_keep_the_non_create_event_query() => _methods.Any(_ => _.Name == "From").ShouldBeTrue();
    [Fact] void should_keep_the_three_parameter_query() => _methods.Any(_ => _.GetParameters().Length == 3).ShouldBeTrue();
    [Fact] void should_keep_the_collection_query() => _methods.Any(_ => _.ReturnType.IsGenericType && _.ReturnType.GetGenericTypeDefinition().FullName == "System.Collections.Generic.IEnumerable`1").ShouldBeTrue();
    [Fact] void should_not_expose_a_fold_only_read_model() => _foldOnly.IsQuery().ShouldBeFalse();
    [Fact] void should_match_runtime_discovery() => _methods.Select(Signature).ShouldContainOnly(typeof(FoldReadModel).GetQueryMethods().Select(Signature).ToArray());
    [Fact] void should_leave_queries_unchanged_when_a_parameter_base_is_unresolvable() => _methodsWithUnresolvableMetadata.Select(Signature).ShouldContainOnly(_methods.Select(Signature).ToArray());

    bool HasCreate(string parameter) => _methods.Any(_ => _.Name == "Create" && _.GetParameters().Length == 1 && _.GetParameters()[0].ParameterType.FullName == parameter);
    static string Signature(MethodInfo method) => $"{method.Name}({string.Join(',', method.GetParameters().Select(_ => _.ParameterType.FullName))})";
}
