// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices.for_SliceLocationResolver.when_resolving;

public class with_a_namespace_root_that_is_a_prefix_of_a_sibling : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = SliceLocationResolver.Resolve("MyApp.FeaturesLegacy.Billing.Invoices.Pay", ["MyApp.Features"], 2);

    [Fact] void should_resolve_the_module() => _result.Module.ShouldEqual(new ModuleName("Billing"));
    [Fact] void should_resolve_the_feature() => _result.Feature.ShouldEqual(new FeatureName("Invoices"));
    [Fact] void should_resolve_the_slice() => _result.Slice.ShouldEqual(new SliceName("Pay"));
}
