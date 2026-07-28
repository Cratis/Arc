// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Holds the stable codes every <see cref="ScreenplayDiagnostic"/> is identified by.
/// </summary>
/// <remarks>
/// Codes are never reused and never renumbered - consumers suppress and group on them.
/// </remarks>
public static class ScreenplayDiagnosticCodes
{
    /// <summary>
    /// Source analysis has not produced a model.
    /// </summary>
    public const string AnalysisUnavailable = "SP0001";

    /// <summary>
    /// A slice declared nothing that can be expressed and was left out.
    /// </summary>
    public const string EmptySlice = "SP0002";

    /// <summary>
    /// A concept declared an enumeration without any values and was left out.
    /// </summary>
    public const string EnumWithoutValues = "SP0003";

    /// <summary>
    /// A projection produced no directives at all and was left out.
    /// </summary>
    public const string EmptyProjection = "SP0004";

    /// <summary>
    /// A projection property expression has no counterpart in the projection definition language.
    /// </summary>
    public const string UnmappableProjectionExpression = "SP0005";

    /// <summary>
    /// A projection key has no counterpart in the projection definition language.
    /// </summary>
    public const string UnmappableProjectionKey = "SP0006";

    /// <summary>
    /// A child or nested projection scope could not be expressed and was left out.
    /// </summary>
    public const string UnmappableProjectionScope = "SP0007";

    /// <summary>
    /// A join declared no property to key on and was left out.
    /// </summary>
    public const string UnmappableProjectionJoin = "SP0008";

    /// <summary>
    /// A reactor observed no events and was left out.
    /// </summary>
    public const string ReactorWithoutEvents = "SP0009";

    /// <summary>
    /// A concurrency scope narrowed nothing at all and was left out.
    /// </summary>
    public const string EmptyConcurrencyScope = "SP0010";

    /// <summary>
    /// A validation rule requires an operand it was not given, and was left out.
    /// </summary>
    public const string ValidationRuleWithoutOperand = "SP0011";

    /// <summary>
    /// A command handler produces events in a way that could not be read from the source.
    /// </summary>
    public const string UnmappableCommandProduction = "SP0012";

    /// <summary>
    /// A command handler yields the identifier of the event source it appends to, which Screenplay cannot express.
    /// </summary>
    public const string UnmappableEventSourceIdResult = "SP0013";

    /// <summary>
    /// An event declares generations, a tombstone or a compensation, none of which Screenplay has a counterpart for.
    /// </summary>
    public const string EventFeatureWithoutCounterpart = "SP0014";

    /// <summary>
    /// A projection declares something the projection definition language has no counterpart for.
    /// </summary>
    public const string UnmappableProjectionConstruct = "SP0015";

    /// <summary>
    /// A validator declares a rule that could not be expressed declaratively.
    /// </summary>
    public const string UnmappableValidationRule = "SP0016";

    /// <summary>
    /// A constraint declares a rule that is neither unique on a property nor unique on an event type.
    /// </summary>
    public const string UnmappableConstraint = "SP0017";

    /// <summary>
    /// An aggregate root produces events from code, which Screenplay has no counterpart for.
    /// </summary>
    public const string AggregateRootWithoutCounterpart = "SP0018";

    /// <summary>
    /// A query returns something that could not be expressed as a Screenplay type reference.
    /// </summary>
    public const string UnmappableQuery = "SP0019";

    /// <summary>
    /// A reducer folds events into a read model, which Screenplay has no counterpart for.
    /// </summary>
    public const string ReducerWithoutCounterpart = "SP0020";

    /// <summary>
    /// An event referenced by the application is declared outside the compilation being analyzed.
    /// </summary>
    public const string EventDeclaredOutsideCompilation = "SP0021";

    /// <summary>
    /// More than one query in a slice is declared under the same name.
    /// </summary>
    public const string AmbiguousQueryName = "SP0022";

    /// <summary>
    /// A namespace carries no structure to arrange the document by, so several levels take the same name.
    /// </summary>
    public const string NamespaceWithoutStructure = "SP0023";

    /// <summary>
    /// The source did not compile, so nothing recovered from it can be relied on.
    /// </summary>
    public const string SourceDidNotCompile = "SP0024";

    /// <summary>
    /// A user interface file sits alongside the source of a slice whose relationship to it is not certain.
    /// </summary>
    /// <remarks>
    /// A screen is recovered as a <c>file</c> reference and nothing more. The declarative form - <c>data</c> via a
    /// query, <c>section</c>, <c>table</c> and <c>summary</c> with their columns and fields, <c>action</c> and
    /// <c>navigate to</c> - is never inferred, because it would have to be read out of TypeScript and JSX rather
    /// than out of the compilation, and a guess at a screen's structure is worse than saying which file realizes it.
    /// Which file that is comes from where the file sits, so this reports every case where sitting there says less
    /// than usual: source spread over several folders, one folder holding the source of several slices, and two
    /// files claiming one screen name.
    /// </remarks>
    public const string AmbiguousScreenFile = "SP0025";

    /// <summary>
    /// A named authorization policy is referred to, but what it requires of the caller could not be recovered.
    /// </summary>
    /// <remarks>
    /// The name of a policy sits on the artifact, while what it requires sits where the application is composed.
    /// When the registration cannot be found or cannot be read, the document still declares the policy - a reference
    /// to something undeclared is a document that warns - but it declares the least it can say rather than a guess.
    /// </remarks>
    public const string PolicyRequirementsUnrecoverable = "SP0026";
}
