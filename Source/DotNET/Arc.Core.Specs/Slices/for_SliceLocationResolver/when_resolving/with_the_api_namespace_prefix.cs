// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices.for_SliceLocationResolver.when_resolving;

public class with_the_api_namespace_prefix : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = SliceLocationResolver.Resolve("Cratis.API.Billing.Invoices.Pay", [], 0);

    [Fact] void should_not_include_the_prefix_in_the_module() => _result.Module.ShouldEqual(new ModuleName("Billing"));
    [Fact] void should_resolve_the_feature() => _result.Feature.ShouldEqual(new FeatureName("Invoices"));
    [Fact] void should_resolve_the_slice() => _result.Slice.ShouldEqual(new SliceName("Pay"));
}
