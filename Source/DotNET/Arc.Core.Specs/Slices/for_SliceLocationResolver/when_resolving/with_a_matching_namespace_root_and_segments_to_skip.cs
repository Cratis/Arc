// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices.for_SliceLocationResolver.when_resolving;

public class with_a_matching_namespace_root_and_segments_to_skip : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = SliceLocationResolver.Resolve("MyApp.Features.Billing.Invoices.Pay", ["MyApp.Features"], 3);

    [Fact] void should_resolve_the_module() => _result.Module.ShouldEqual(new ModuleName("Billing"));
    [Fact] void should_resolve_the_feature() => _result.Feature.ShouldEqual(new FeatureName("Invoices"));
    [Fact] void should_resolve_the_slice() => _result.Slice.ShouldEqual(new SliceName("Pay"));
}
