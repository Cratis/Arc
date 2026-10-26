// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices.for_SliceLocationResolver.when_resolving;

public class with_one_segment : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = SliceLocationResolver.Resolve("MyApp.Features.Pay", ["MyApp.Features"], 0);

    [Fact] void should_leave_the_module_empty() => _result.Module.ShouldEqual(ModuleName.NotSet);
    [Fact] void should_leave_the_feature_empty() => _result.Feature.ShouldEqual(FeatureName.NotSet);
    [Fact] void should_resolve_the_slice() => _result.Slice.ShouldEqual(new SliceName("Pay"));
}
