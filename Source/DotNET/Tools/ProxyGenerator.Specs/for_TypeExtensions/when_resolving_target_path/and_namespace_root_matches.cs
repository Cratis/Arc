// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_resolving_target_path;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_namespace_root_matches : Specification
{
    class TypeInSubNamespace;

    string _result = string.Empty;

    void Establish()
    {
        var ns = typeof(TypeInSubNamespace).Namespace!;

        // Use the parent namespace as root so the last segment becomes part of the expected path
        var rootNs = ns[..ns.LastIndexOf('.')];
        TypeExtensions.SetNamespaceRoots([(rootNs, "generated")]);
    }

    void Because() => _result = typeof(TypeInSubNamespace).ResolveTargetPath(0);

    void Destroy() => TypeExtensions.SetNamespaceRoots([]);

    [Fact] void should_include_base_folder_as_prefix() =>
        _result.ShouldEqual(Path.Join("generated", "when_resolving_target_path"));
}
