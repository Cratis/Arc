// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Holds the fully qualified metadata names of every framework type source analysis recognizes.
/// </summary>
/// <remarks>
/// Matching is by name rather than by symbol identity so that the generator never has to reference Arc, Chronicle
/// or ASP.NET Core itself. Generic types carry their arity, which is what <c>MetadataName</c> yields.
/// </remarks>
public static class WellKnownTypeNames
{
    /// <summary>The attribute marking a model-bound command.</summary>
    public const string CommandAttribute = "Cratis.Arc.Commands.ModelBound.CommandAttribute";

    /// <summary>The attribute marking a model-bound read model.</summary>
    public const string ReadModelAttribute = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";

    /// <summary>The attribute requiring an authenticated caller.</summary>
    public const string AuthorizeAttribute = "Cratis.Arc.Authorization.AuthorizeAttribute";

    /// <summary>The attribute naming the roles a caller may hold.</summary>
    public const string RolesAttribute = "Cratis.Arc.Authorization.RolesAttribute";

    /// <summary>The attribute allowing anonymous callers.</summary>
    public const string AllowAnonymousAttribute = "Cratis.Arc.Authorization.AllowAnonymousAttribute";

    /// <summary>The options every named authorization policy is registered on.</summary>
    public const string AuthorizationOptions = "Microsoft.AspNetCore.Authorization.AuthorizationOptions";

    /// <summary>The builder every named authorization policy can also be registered on.</summary>
    public const string AuthorizationBuilder = "Microsoft.AspNetCore.Authorization.AuthorizationBuilder";

    /// <summary>The builder the requirements of a named authorization policy are declared against.</summary>
    public const string AuthorizationPolicyBuilder = "Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder";

    /// <summary>The interface a command pipeline is executed through.</summary>
    public const string CommandPipeline = "Cratis.Arc.Commands.ICommandPipeline";

    /// <summary>The base type every strongly typed domain value derives from.</summary>
    public const string ConceptAs = "Cratis.Concepts.ConceptAs`1";

    /// <summary>The attribute marking an event type.</summary>
    public const string EventTypeAttribute = "Cratis.Chronicle.Events.EventTypeAttribute";

    /// <summary>The attribute classifying an event with tags.</summary>
    public const string TagAttribute = "Cratis.Chronicle.TagAttribute";

    /// <summary>The attribute classifying an event with tags, plural form.</summary>
    public const string TagsAttribute = "Cratis.Chronicle.TagsAttribute";

    /// <summary>The attribute marking an event as a tombstone.</summary>
    public const string TombstoneAttribute = "Cratis.Chronicle.Events.TombstoneAttribute";

    /// <summary>The attribute marking an event as compensating another.</summary>
    public const string CompensationForAttribute = "Cratis.Chronicle.Events.CompensationForAttribute";

    /// <summary>The generic attribute marking an event as compensating another.</summary>
    public const string CompensationForAttributeOfT = "Cratis.Chronicle.Events.CompensationForAttribute`1";

    /// <summary>The attribute marking a value as personally identifiable information.</summary>
    public const string PiiAttribute = "Cratis.Chronicle.Compliance.GDPR.PIIAttribute";

    /// <summary>The attribute narrowing a command to an event source type.</summary>
    public const string EventSourceTypeAttribute = "Cratis.Chronicle.Events.EventSourceTypeAttribute";

    /// <summary>The attribute narrowing a command to an event stream type.</summary>
    public const string EventStreamTypeAttribute = "Cratis.Chronicle.Events.EventStreamTypeAttribute";

    /// <summary>The attribute narrowing a command to an event stream identifier.</summary>
    public const string EventStreamIdAttribute = "Cratis.Chronicle.Events.EventStreamIdAttribute";

    /// <summary>The attribute requiring a property of an event to be unique.</summary>
    public const string UniqueAttribute = "Cratis.Chronicle.Events.Constraints.UniqueAttribute";

    /// <summary>The interface a constraint declared in code implements.</summary>
    public const string Constraint = "Cratis.Chronicle.Events.Constraints.IConstraint";

    /// <summary>The interface a fluent projection implements.</summary>
    public const string ProjectionFor = "Cratis.Chronicle.Projections.IProjectionFor`1";

    /// <summary>The attribute configuring a projection.</summary>
    public const string ProjectionAttribute = "Cratis.Chronicle.Projections.ProjectionAttribute";

    /// <summary>The interface a reducer implements.</summary>
    public const string ReducerFor = "Cratis.Chronicle.Reducers.IReducerFor`1";

    /// <summary>The marker interface a reactor implements.</summary>
    public const string Reactor = "Cratis.Chronicle.Reactors.IReactor";

    /// <summary>The attribute configuring a reactor.</summary>
    public const string ReactorAttribute = "Cratis.Chronicle.Reactors.ReactorAttribute";

    /// <summary>The base type an aggregate root derives from.</summary>
    public const string AggregateRoot = "Cratis.Arc.Chronicle.Aggregates.AggregateRoot";

    /// <summary>The interface every aggregate root implements.</summary>
    public const string AggregateRootInterface = "Cratis.Arc.Chronicle.Aggregates.IAggregateRoot";

    /// <summary>The base type every FluentValidation validator derives from.</summary>
    public const string AbstractValidator = "FluentValidation.AbstractValidator`1";

    /// <summary>The base type of an ASP.NET Core controller.</summary>
    public const string ControllerBase = "Microsoft.AspNetCore.Mvc.ControllerBase";

    /// <summary>The attribute marking a controller method as taking its argument from the request body.</summary>
    public const string FromBodyAttribute = "Microsoft.AspNetCore.Mvc.FromBodyAttribute";

    /// <summary>The interface every transport level result of a controller method implements.</summary>
    public const string ActionResultInterface = "Microsoft.AspNetCore.Mvc.IActionResult";

    /// <summary>The base type the transport level results of a controller method derive from.</summary>
    public const string ActionResult = "Microsoft.AspNetCore.Mvc.ActionResult";

    /// <summary>The transport level result carrying the value a controller method really returns.</summary>
    public const string ActionResultOfT = "Microsoft.AspNetCore.Mvc.ActionResult`1";

    /// <summary>The token a query is handed to observe cancellation through.</summary>
    public const string CancellationToken = "System.Threading.CancellationToken";

    /// <summary>The everything a query is performed with, which the host fills in.</summary>
    public const string QueryContext = "Cratis.Arc.Queries.QueryContext";

    /// <summary>The sequence a specification appends the events it starts from to.</summary>
    public const string EventSequence = "Cratis.Chronicle.EventSequences.IEventSequence";

    /// <summary>The scenario a specification issues a command through in process.</summary>
    public const string CommandScenario = "Cratis.Arc.Testing.Commands.CommandScenario`1";

    /// <summary>The builder a specification states the state of one event source with.</summary>
    public const string CommandScenarioSourceGivenBuilder = "Cratis.Arc.Chronicle.Testing.Commands.CommandScenarioSourceGivenBuilder`1";

    /// <summary>The extensions a specification issues a command through over HTTP.</summary>
    public const string HttpClientExtensions = "Cratis.Chronicle.XUnit.Integration.HttpClientExtensions";

    /// <summary>The attribute marking a method as one assertion of a specification.</summary>
    public const string FactAttribute = "Xunit.FactAttribute";

    /// <summary>The page of a result a query is performed for, which the host fills in from the request.</summary>
    public const string Paging = "Cratis.Arc.Queries.Paging";

    /// <summary>The order a result is returned in, which the host fills in from the request.</summary>
    public const string Sorting = "Cratis.Arc.Queries.Sorting";
}
