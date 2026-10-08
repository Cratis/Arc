// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model holding two different records that share a simple name. A type is declared under that name once, so
/// one of the properties would be written with a name declaring the other's shape - the read model is not declared,
/// and neither record is declared on its behalf.
/// </summary>
public class a_read_model_holding_two_records_sharing_a_name : a_read_model_document
{
    const string Billing = """
        namespace Library.Billing;

        public record Address(string Street);
        """;

    const string Shipping = """
        namespace Library.Shipping;

        public record Address(int Zone);
        """;

    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Customers.Listing;

        [ReadModel]
        public record Customer(string Name, Library.Billing.Address BillTo, Library.Shipping.Address ShipTo)
        {
            public static IEnumerable<Customer> All() => [];
        }
        """;

    void Because() => Generate(
        ("Library/Billing/Address.cs", Billing),
        ("Library/Shipping/Address.cs", Shipping),
        ("Library/Customers/Listing/Listing.cs", Source));

    [Fact] void should_not_declare_the_read_model() => Count("readmodel Customer").ShouldEqual(0);
    [Fact] void should_not_declare_either_record_for_it() => Result.Source.ShouldNotContain("type Address");
    [Fact] void should_say_which_property() => LeftOut.Single().ShouldContain("'ShipTo'");
    [Fact] void should_report_no_warning() => GenerationWarnings.ShouldBeEmpty();
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}
