// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// The sources the availability specifications generate a document from.
/// </summary>
public static class AvailabilitySources
{
    /// <summary>
    /// The path the slice is compiled as.
    /// </summary>
    public const string Path = "Library/Fleet/DataQuality/DataQuality.cs";

    /// <summary>
    /// Wraps a declaration in a slice with a vessel read model and its query.
    /// </summary>
    /// <param name="declarations">The commands, read models and registrations to add.</param>
    /// <returns>The source of the slice.</returns>
    public static string With(string declarations) => """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Availability;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Fleet.DataQuality;

        public record VesselId(Guid Value) : EventSourceId<Guid>(Value);

        [EventType]
        public record ScoreReset(VesselId Vessel);

        [EventType]
        public record VesselRegistered(string Name);

        """ + declarations;

    /// <summary>
    /// A read model whose query a value can be resolved by.
    /// </summary>
    public const string Vessels = """
        [ReadModel]
        [FromEvent<VesselRegistered>]
        public record Vessel([Key] Guid VesselId, string Name)
        {
            public static Vessel? VesselByKey(Guid vesselId) => null;
        }
        """;

    /// <summary>
    /// The shapes of the host libraries a host registers gates and values with.
    /// </summary>
    /// <remarks>
    /// The libraries are not references of the specifications: Choreography brings the ASP.NET Core shared framework along,
    /// which makes every compilation the specifications build see its types twice. Analysis matches the registrations by
    /// the full names of these types, so source declaring them under the same names is read the way the real ones are.
    /// </remarks>
    public const string HostLibraries = """
        namespace Cratis.Choreography.Availability.Context
        {
            public class ContextValueCatalog
            {
                public ContextValueCatalog Declare(ContextValueDeclaration declaration) => this;
            }

            public interface IContextValueProvider;

            public record ContextValueDeclaration(string Name, IContextValueProvider Provider, ContextScope Scope, bool Required = true);

            public class ContextScope
            {
                public static ContextScope Application { get; } = new();

                public static ContextScope Of(params string[] path) => new();
            }
        }

        namespace Cratis.Choreography.Availability.Context.Providers
        {
            public interface IReadModelQuery;

            public interface IHostContextValues;

            public class ReadModelContextValueProvider(IReadModelQuery query, string queryName, string key) : IContextValueProvider;

            public class HostContextValueProvider(IHostContextValues values, string name) : IContextValueProvider;
        }

        namespace Cratis.Arc.Availability
        {
            public class ArcAvailabilityOptions
            {
                public ArcAvailabilityOptions GateNamespace(string @namespace, object gate) => this;
            }
        }
        """;
}
