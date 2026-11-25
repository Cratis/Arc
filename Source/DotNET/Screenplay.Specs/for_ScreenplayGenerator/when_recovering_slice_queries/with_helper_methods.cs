// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_slice_queries;

public class with_helper_methods : a_generated_document
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.ReadModels;
        namespace Library.Invoices.InvoiceLookup;
        [EventType] public record InvoiceRaised(string InvoiceId);
        public record Other(string Id);
        [ReadModel]
        public record InvoiceView([Key] string InvoiceId)
        {
            public static async Task<InvoiceView?> InvoiceById(IReadModels readModels, EventSourceId invoiceId) =>
                await readModels.GetInstanceById<InvoiceView>(invoiceId);

            static async Task<InvoiceView?> PrivateHelper(IReadModels readModels, EventSourceId invoiceId) =>
                await readModels.GetInstanceById<InvoiceView>(invoiceId);

            public async Task<InvoiceView?> InstanceHelper(IReadModels readModels, EventSourceId invoiceId) =>
                await readModels.GetInstanceById<InvoiceView>(invoiceId);

            public static Other? ForeignHelper(IReadModels readModels, EventSourceId invoiceId) => null;
        }
        """;

    void Because() => Generate(("Library/Program.cs", "namespace Library;"), ("Library/Invoices/InvoiceLookup/InvoiceLookup.cs", Source));

    [Fact] void should_recover_only_the_query() => Result.Model.Slices.Single(slice => slice.Name == "InvoiceLookup").Queries.Select(query => query.Name).ShouldContainOnly("InvoiceById");
}
