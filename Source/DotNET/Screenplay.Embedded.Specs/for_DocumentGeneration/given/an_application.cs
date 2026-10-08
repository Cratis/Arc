// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.given;

/// <summary>
/// An application shaped the way the hierarchy has to deal with - a module with features nested in it, a feature
/// sitting directly under the root namespace, a slice in each, and references that cross every one of those
/// boundaries.
/// </summary>
/// <remarks>
/// It is built by hand rather than compiled, because what is being specified is how a recovered model is
/// partitioned and emitted - not how it is recovered. The emitter and the Screenplay compiler the specifications
/// run are the real ones, so a document that is asserted to compile really does.
/// </remarks>
public static class an_application
{
    /// <summary>
    /// The name of the assembly the documents are embedded in.
    /// </summary>
    public const string AssemblyName = "Library";

    /// <summary>
    /// The namespace the hierarchy is resolved relative to.
    /// </summary>
    public const string RootNamespace = "Library";

    /// <summary>
    /// The namespace of the module.
    /// </summary>
    public const string Module = "Library.Accounting";

    /// <summary>
    /// The namespace of the feature within the module.
    /// </summary>
    public const string Feature = "Library.Accounting.Invoices";

    /// <summary>
    /// The namespace of the feature nested within that feature.
    /// </summary>
    public const string NestedFeature = "Library.Accounting.Invoices.Payments";

    /// <summary>
    /// The namespace of the feature sitting directly under the root namespace.
    /// </summary>
    public const string RootedFeature = "Library.Authors";

    /// <summary>
    /// The name of the event declared beneath the rooted feature.
    /// </summary>
    public const string EventOfTheRootedFeature = "AuthorRegistered";

    /// <summary>
    /// The name of the event declared beneath the feature of the module.
    /// </summary>
    public const string EventOfTheFeature = "InvoiceIssued";

    /// <summary>
    /// Builds the model of the application.
    /// </summary>
    /// <returns>The <see cref="ApplicationModel"/>.</returns>
    public static ApplicationModel Build() =>
        new(
            AssemblyName,
            AssemblyName,
            [],
            [],
            Slices(),
            []);

    /// <summary>
    /// Builds the options the documents of the application are generated with.
    /// </summary>
    /// <returns>The <see cref="Generation.EmbeddedDocumentOptions"/>.</returns>
    public static Generation.EmbeddedDocumentOptions Options() => new(AssemblyName, RootNamespace);

    /// <summary>
    /// Declares every slice of the application.
    /// </summary>
    /// <returns>The slices.</returns>
    public static IReadOnlyList<SliceModel> Slices() =>
    [
        Registration(),
        Issuing(),
        Settling(),
        Reporting(),
        Housekeeping()
    ];

    /// <summary>
    /// Declares the slice beneath the feature that sits directly under the root namespace.
    /// </summary>
    /// <returns>The <see cref="SliceModel"/>.</returns>
    public static SliceModel Registration() =>
        SliceModel.Empty($"{RootedFeature}.Registration", "Registration", SliceKind.StateChange) with
        {
            Commands = [Command("RegisterAuthor", EventOfTheRootedFeature)],
            Events = [Event(EventOfTheRootedFeature)]
        };

    /// <summary>
    /// Declares the slice beneath the feature of the module.
    /// </summary>
    /// <returns>The <see cref="SliceModel"/>.</returns>
    public static SliceModel Issuing() =>
        SliceModel.Empty($"{Feature}.Issuing", "Issuing", SliceKind.StateChange) with
        {
            Commands = [Command("IssueInvoice", EventOfTheFeature)],
            Events = [Event(EventOfTheFeature)]
        };

    /// <summary>
    /// Declares the slice beneath the nested feature, which refers to events declared in two other scopes.
    /// </summary>
    /// <returns>The <see cref="SliceModel"/>.</returns>
    public static SliceModel Settling() =>
        SliceModel.Empty($"{NestedFeature}.Settling", "Settling", SliceKind.Automation) with
        {
            Reactors = [new("Settlement", [EventOfTheFeature, EventOfTheRootedFeature], false, null)]
        };

    /// <summary>
    /// Declares the slice sitting directly in the module, with no feature between.
    /// </summary>
    /// <returns>The <see cref="SliceModel"/>.</returns>
    public static SliceModel Reporting() =>
        SliceModel.Empty($"{Module}.Reporting", "Reporting", SliceKind.StateChange) with
        {
            Commands = [Command("CloseBooks", "BooksClosed")],
            Events = [Event("BooksClosed")]
        };

    /// <summary>
    /// Declares the slice sitting directly in the root namespace, which belongs to no module and no feature.
    /// </summary>
    /// <returns>The <see cref="SliceModel"/>.</returns>
    public static SliceModel Housekeeping() =>
        SliceModel.Empty($"{RootNamespace}.Housekeeping", "Housekeeping", SliceKind.StateChange) with
        {
            Commands = [Command("Tidy", "TidiedUp")],
            Events = [Event("TidiedUp")]
        };

    /// <summary>
    /// Declares a command producing one event.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="event">The name of the event it produces.</param>
    /// <returns>The <see cref="CommandModel"/>.</returns>
    static CommandModel Command(string name, string @event) =>
        new(name, null, [Property("Reference")], null, [], [new(@event, null, [new("Reference", new PropertyPathSource("Reference"))]) { UsesCommandContext = true }], null, null);

    /// <summary>
    /// Declares an event carrying one property.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <returns>The <see cref="EventModel"/>.</returns>
    static EventModel Event(string name) => new(name, [Property("Reference")], []);

    /// <summary>
    /// Declares a property carrying a string.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <returns>The <see cref="PropertyModel"/>.</returns>
    static PropertyModel Property(string name) => new(name, new("String", false, false));
}
