// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Company.Product.Features.Billing.Invoices.Pay;

namespace Cratis.Arc.Slices.for_SliceLocation.when_building_a_consuming_assembly;

/// <summary>
/// The SliceLocationConsumer fixture is built with NamespaceRoot items and CratisProxiesSegmentsToSkip, importing
/// Cratis.Arc.Core.targets the way the package does; this proves the targets emit the attributes into its assembly.
/// </summary>
public class with_namespace_roots_and_segments_to_skip : Specification
{
    Assembly _assembly = null!;
    SliceLocation _result = null!;

    void Because()
    {
        _assembly = typeof(PayInvoice).Assembly;
        _result = SliceLocation.For(typeof(PayInvoice));
    }

    [Fact] void should_emit_a_namespace_root_attribute_for_every_namespace_root() =>
        _assembly.GetCustomAttributes<SliceNamespaceRootAttribute>().Select(_ => _.Namespace).ShouldContainOnly("Company.Product.Features", "Company.Product.Admin");
    [Fact] void should_emit_the_segments_to_skip() => _assembly.GetCustomAttribute<SliceNamespaceSegmentsToSkipAttribute>()!.Count.ShouldEqual(2);
    [Fact] void should_resolve_the_slice_location_from_the_emitted_attributes() => _result.ShouldEqual(new SliceLocation("Billing", "Invoices", "Pay"));
}
