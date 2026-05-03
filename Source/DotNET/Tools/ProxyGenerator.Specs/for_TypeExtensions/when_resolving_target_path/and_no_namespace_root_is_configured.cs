// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_resolving_target_path;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_no_namespace_root_is_configured : Specification
{
    class SomeType;

    string _result = string.Empty;

    void Establish() => TypeExtensions.SetNamespaceRoots([]);

    void Because() => _result = typeof(SomeType).ResolveTargetPath(0);

    [Fact] void should_produce_a_non_empty_path() => _result.ShouldNotBeNull();
}
