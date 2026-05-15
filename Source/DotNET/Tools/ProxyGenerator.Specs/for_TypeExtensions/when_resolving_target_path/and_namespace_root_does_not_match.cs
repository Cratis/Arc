// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_resolving_target_path;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_namespace_root_does_not_match : Specification
{
    class TypeOutsideRoot;

    string _result = string.Empty;

    void Establish() =>
        TypeExtensions.SetNamespaceRoots([("Some.Completely.Different.Namespace", "generated")]);

    void Because() => _result = typeof(TypeOutsideRoot).ResolveTargetPath(0);

    void Destroy() => TypeExtensions.SetNamespaceRoots([]);

    [Fact] void should_not_include_base_folder_in_path() => _result.ShouldNotContain("generated");
}
