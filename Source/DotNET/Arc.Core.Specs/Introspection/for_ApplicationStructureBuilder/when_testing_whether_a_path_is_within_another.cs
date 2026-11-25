// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_ApplicationStructureBuilder;

public class when_testing_whether_a_path_is_within_another : Specification
{
    static readonly string[] _path = ["Billing", "Invoices", "Pay"];

    [Fact] void should_be_within_itself() => ApplicationStructureBuilder.IsWithin(_path, "Billing/Invoices/Pay").ShouldBeTrue();
    [Fact] void should_be_within_its_module() => ApplicationStructureBuilder.IsWithin(_path, "Billing").ShouldBeTrue();
    [Fact] void should_be_within_its_feature() => ApplicationStructureBuilder.IsWithin(_path, "Billing/Invoices").ShouldBeTrue();
    [Fact] void should_not_be_within_a_sibling() => ApplicationStructureBuilder.IsWithin(_path, "Billing/Payments").ShouldBeFalse();
    [Fact] void should_not_be_within_a_longer_path() => ApplicationStructureBuilder.IsWithin(_path, "Billing/Invoices/Pay/More").ShouldBeFalse();
    [Fact] void should_not_match_on_a_partial_segment() => ApplicationStructureBuilder.IsWithin(_path, "Bill").ShouldBeFalse();
    [Fact] void should_not_be_within_an_empty_path() => ApplicationStructureBuilder.IsWithin(_path, string.Empty).ShouldBeFalse();
}
