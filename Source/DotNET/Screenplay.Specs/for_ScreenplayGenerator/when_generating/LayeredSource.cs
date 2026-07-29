// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The source of a small application written as two projects, the way a layered Arc application really is written.
/// </summary>
/// <remarks>
/// A contracts project publishes the events of a bounded context and the project beside it handles the commands that
/// produce them, which is the arrangement a single project generation described half of. The events live in the
/// namespace of the slice they belong to, so the command and the events it produces are one slice written from two
/// compilations - and a second slice is declared by the application project alone, so a document holding only one of
/// the projects would be visibly short either way round.
/// </remarks>
public static class LayeredSource
{
    /// <summary>
    /// The name of the assembly the contracts project builds.
    /// </summary>
    public const string ContractsAssembly = "Library.Contracts";

    /// <summary>
    /// The name of the assembly the application project builds.
    /// </summary>
    public const string ApplicationAssembly = "Library";

    const string Contracts = """
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Ordering.Placing;

        public record OrderReference(string Value) : ConceptAs<string>(Value);

        [PII("The name of a person")]
        public record CustomerName(string Value) : ConceptAs<string>(Value);

        /// <summary>
        /// An order was placed by a customer.
        /// </summary>
        [EventType]
        public record OrderPlaced(OrderReference Reference, CustomerName Customer);
        """;

    const string Placing = """
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Ordering.Placing;

        /// <summary>
        /// Places an order for a customer.
        /// </summary>
        [Command]
        public record PlaceOrder(OrderReference Reference, CustomerName Customer)
        {
            public OrderPlaced Handle() => new(Reference, Customer);
        }
        """;

    const string Dispatching = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        using Library.Ordering.Placing;

        namespace Library.Shipping.Dispatching;

        public class Dispatcher : IReactor
        {
            public Task Dispatch(OrderPlaced @event, EventContext context) => Task.CompletedTask;
        }
        """;

    const string Listing = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Projections;
        using Library.Ordering.Placing;

        namespace Library.Ordering.Listing;

        [ReadModel]
        public record Order
        {
            public string Id { get; init; } = string.Empty;

            public OrderReference Reference { get; init; } = new(string.Empty);

            public static IEnumerable<Order> AllOrders() => [];
        }

        public class OrderProjection : IProjectionFor<Order>
        {
            public void Define(IProjectionBuilderFor<Order> builder) => builder
                .AutoMap()
                .From<OrderPlaced>(_ => _
                    .Set(m => m.Id).ToEventSourceId()
                    .Set(m => m.Reference).To(e => e.Reference));
        }
        """;

    /// <summary>
    /// Builds the contracts project, which declares the events of the ordering context.
    /// </summary>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public static Compilation ContractsProject() =>
        Analyzed.Project(
            ContractsAssembly,
            [],
            ("Source/Library.Contracts/Contracts.cs", "namespace Library.Contracts;"),
            ("Source/Library.Contracts/Ordering/Placing/Placing.cs", Contracts));

    /// <summary>
    /// Builds the application project, which handles the commands of the ordering context.
    /// </summary>
    /// <param name="contracts">The contracts project it references.</param>
    /// <returns>The <see cref="Compilation"/>.</returns>
    /// <remarks>
    /// The reference is the compilation itself rather than an emitted image, which is what a project reference within
    /// one solution really is once a workspace has loaded it - and is the shape in which a sibling project looks
    /// exactly like the referenced package an import exists for.
    /// </remarks>
    public static Compilation ApplicationProject(Compilation contracts) =>
        Analyzed.Project(
            ApplicationAssembly,
            [contracts.ToMetadataReference()],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Ordering/Placing/Placing.cs", Placing),
            ("Source/Library/Ordering/Listing/Listing.cs", Listing),
            ("Source/Library/Shipping/Dispatching/Dispatching.cs", Dispatching));
}
