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
    /// <summary>
    /// The typed availability declarations of the host, as source with the shapes the real types have.
    /// </summary>
    /// <remarks>
    /// The generator reads the declarations by the full names of the types and the shapes of their members, so source declaring
    /// them is read the way the real ones are, without referencing the engine, which would make every compilation see them twice.
    /// </remarks>
    public const string TypedLibraries = """
        namespace Cratis.Arc.Availability
        {
            public abstract class Audience;

            public abstract class AvailabilityScope;

            public interface IAvailabilityCondition;

            public abstract class AvailabilityTerm
            {
                public static AvailabilityTerm operator &(AvailabilityTerm left, AvailabilityTerm right) => left;

                public static AvailabilityTerm operator |(AvailabilityTerm left, AvailabilityTerm right) => left;
            }

            public abstract class ConditionTerm : AvailabilityTerm
            {
                public static ConditionTerm operator &(ConditionTerm left, ConditionTerm right) => left;

                public static ConditionTerm operator |(ConditionTerm left, ConditionTerm right) => left;
            }

            public sealed class ContextOperand
            {
                public ConditionTerm IsTrue() => null!;

                public ConditionTerm Is(object value) => null!;

                public ConditionTerm IsNot(object value) => null!;

                public ConditionTerm In(params object[] values) => null!;

                public ConditionTerm NotIn(params object[] values) => null!;

                public ConditionTerm IsGreaterThan(object value) => null!;

                public ConditionTerm IsAtLeast(object value) => null!;

                public ConditionTerm IsLessThan(object value) => null!;

                public ConditionTerm IsAtMost(object value) => null!;
            }

            public sealed class AvailabilityWhen
            {
                public ContextOperand On<TReadModel>(System.Linq.Expressions.Expression<System.Func<TReadModel, object?>> value) => null!;

                public ConditionTerm Condition<TCondition>()
                    where TCondition : IAvailabilityCondition => null!;
            }

            public abstract class AvailabilityBlock
            {
                protected static AvailabilityWhen When { get; } = new();

                protected abstract System.Collections.Generic.IReadOnlyList<AvailabilityTerm> Lines { get; }

                protected static AvailabilityTerm For<TAudience>()
                    where TAudience : Audience => null!;

                protected static AvailabilityTerm Not<TAudience>()
                    where TAudience : Audience => null!;

                protected static AvailabilityTerm With<TBlock>()
                    where TBlock : AvailabilityBlock => null!;
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public sealed class AvailableForAttribute<TAudience> : System.Attribute
                where TAudience : Audience;

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public sealed class AvailableForAttribute<TAudience1, TAudience2> : System.Attribute
                where TAudience1 : Audience
                where TAudience2 : Audience;

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public sealed class AvailableAttribute<TBlock> : System.Attribute
                where TBlock : AvailabilityBlock;

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public sealed class AvailableWhenAttribute<TCondition> : System.Attribute
                where TCondition : IAvailabilityCondition;

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public sealed class ProvideContextAttribute<TReadModel>(string query, string key) : System.Attribute
            {
                public bool Required { get; set; } = true;
            }
        }
        """;

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
        namespace Cratis.Arc.Availability.Context
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

        namespace Cratis.Arc.Availability.Context.Providers
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
