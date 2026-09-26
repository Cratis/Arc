// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_discovering_metadata_loaded_query_candidates : Specification
{
    MetadataLoadContext _context;
    MethodInfo[] _methods;

    void Establish()
    {
        var assemblyFile = typeof(QueryCandidateReadModel).Assembly.Location;
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
    }

    void Because()
    {
        var assembly = _context.LoadFromAssemblyPath(typeof(QueryCandidateReadModel).Assembly.Location);
        var readModel = assembly.GetType(typeof(QueryCandidateReadModel).FullName!)!;
        _methods = readModel.GetQueryMethods().ToArray();
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_discover_one_query() => _methods.Length.ShouldEqual(1);
    [Fact] void should_discover_the_public_collection_query() => _methods.Single().Name.ShouldEqual(nameof(QueryCandidateReadModel.All));
}
