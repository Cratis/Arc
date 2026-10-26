// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_resolving_slice_location;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_no_namespace_root_matches : Specification
{
    class TypeInSlice;

    SliceLocationDescriptor _result = null!;
    string _outputFolder = string.Empty;

    void Establish() => TypeExtensions.SetNamespaceRoots([("Other.Root", "generated")]);

    void Because()
    {
        _result = typeof(TypeInSlice).ResolveSliceLocation(2);
        _outputFolder = typeof(TypeInSlice).ResolveTargetPath(2);
    }

    void Destroy() => TypeExtensions.SetNamespaceRoots([]);

    [Fact] void should_skip_segments_before_the_module() => _result.Module.ShouldEqual("ProxyGenerator");
    [Fact] void should_use_the_segments_between_as_feature() => _result.Feature.ShouldEqual("for_TypeExtensions");
    [Fact] void should_use_the_last_segment_as_slice() => _result.Slice.ShouldEqual("when_resolving_slice_location");
    [Fact] void should_match_the_output_folder() =>
        _outputFolder.ShouldEqual(Path.Join(_result.Module, _result.Feature, _result.Slice));
}
