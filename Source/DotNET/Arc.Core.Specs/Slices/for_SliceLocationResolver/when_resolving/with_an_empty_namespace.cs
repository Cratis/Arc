// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices.for_SliceLocationResolver.when_resolving;

public class with_an_empty_namespace : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = SliceLocationResolver.Resolve(string.Empty, [], 0);

    [Fact] void should_leave_the_module_empty() => _result.Module.ShouldEqual(ModuleName.NotSet);
    [Fact] void should_leave_the_feature_empty() => _result.Feature.ShouldEqual(FeatureName.NotSet);
    [Fact] void should_leave_the_slice_empty() => _result.Slice.ShouldEqual(SliceName.NotSet);
}
