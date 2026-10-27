// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_resolving_slice_location;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_a_namespace_root_matches : Specification
{
    class TypeInSlice;

    SliceLocationDescriptor _result = null!;
    string _outputFolder = string.Empty;

    void Establish() => TypeExtensions.SetNamespaceRoots([("Cratis.Arc", "generated")]);

    void Because()
    {
        _result = typeof(TypeInSlice).ResolveSliceLocation(1);
        _outputFolder = typeof(TypeInSlice).ResolveTargetPath(1);
    }

    void Destroy() => TypeExtensions.SetNamespaceRoots([]);

    [Fact] void should_use_the_first_segment_below_the_root_as_module() => _result.Module.ShouldEqual("ProxyGenerator");
    [Fact] void should_use_the_segments_between_as_feature() => _result.Feature.ShouldEqual("for_TypeExtensions");
    [Fact] void should_use_the_last_segment_as_slice() => _result.Slice.ShouldEqual("when_resolving_slice_location");
    [Fact] void should_match_the_output_folder() =>
        _outputFolder.ShouldEqual(Path.Join("generated", _result.Module, _result.Feature, _result.Slice));
}
