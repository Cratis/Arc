// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_resolving_slice_location;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_the_namespace_is_too_shallow : Specification
{
    class TypeInSlice;

    SliceLocationDescriptor _result = null!;

    void Establish() => TypeExtensions.SetNamespaceRoots([("Cratis.Arc.ProxyGenerator.for_TypeExtensions", "generated")]);

    void Because() => _result = typeof(TypeInSlice).ResolveSliceLocation(0);

    void Destroy() => TypeExtensions.SetNamespaceRoots([]);

    [Fact] void should_leave_the_module_empty() => _result.Module.ShouldEqual(string.Empty);
    [Fact] void should_leave_the_feature_empty() => _result.Feature.ShouldEqual(string.Empty);
    [Fact] void should_fill_the_slice() => _result.Slice.ShouldEqual("when_resolving_slice_location");
}
