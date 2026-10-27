// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices.for_SliceLocationResolver.when_resolving;

public class without_namespace_roots_or_segments_to_skip : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = SliceLocationResolver.Resolve("MyApp.Billing.Pay", [], 0);

    [Fact] void should_resolve_the_module() => _result.Module.ShouldEqual(new ModuleName("MyApp"));
    [Fact] void should_resolve_the_feature() => _result.Feature.ShouldEqual(new FeatureName("Billing"));
    [Fact] void should_resolve_the_slice() => _result.Slice.ShouldEqual(new SliceName("Pay"));
}
