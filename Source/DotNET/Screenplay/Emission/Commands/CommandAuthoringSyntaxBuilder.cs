// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>Builds grammar-valid, non-executable command intent.</summary>
/// <param name="naming">The naming conventions.</param>
/// <param name="types">The type converter.</param>
public class CommandAuthoringSyntaxBuilder(IScreenplayNaming naming, TypeReferenceConverter types)
{
    readonly MappingSourceConverter _sources = new(naming);
    readonly ConditionConverter _conditions = new(naming);

    /// <summary>Adds optional authoring intent to a command.</summary>
    /// <param name="syntax">The command syntax.</param>
    /// <param name="authoring">The optional authoring intent.</param>
    /// <returns>The command carrying the authoring intent.</returns>
    public CommandSyntax Apply(CommandSyntax syntax, CommandAuthoringModel? authoring) => Apply(syntax, authoring, true);

    /// <summary>
    /// Adds admitted responses and, when enabled, syntax-only command intent.
    /// </summary>
    /// <param name="syntax">The command syntax.</param>
    /// <param name="authoring">The recovered command intent.</param>
    /// <param name="authoringOnlyConstructs">Whether syntax-only intent is enabled.</param>
    /// <returns>The command carrying the selected intent.</returns>
    public CommandSyntax Apply(CommandSyntax syntax, CommandAuthoringModel? authoring, bool authoringOnlyConstructs)
    {
        if (authoring is null)
        {
            return syntax;
        }

        var productions = syntax.Produces.Concat(authoringOnlyConstructs ? authoring.Operations.Select(Operation) : []).ToList();
        var requirements = authoringOnlyConstructs ? authoring.Requirements.Select(Requirement).ToList() : [];
        return syntax with
        {
            Produces = productions,
            Handler = productions.Count > 0 ? null : syntax.Handler,
            Response = Response(authoring),
            Reads = authoringOnlyConstructs ? authoring.Reads.Select(Read).ToList() : syntax.Reads,
            Validations = requirements.Count == 0 ? syntax.Validations : syntax.Validations.Append(new DeclarativeValidateSyntax([], SourceLocation.Start, requirements)).ToList(),
            Stream = authoringOnlyConstructs && authoring.Route is { Stream: not null } route ? Route(route) : syntax.Stream
        };
    }

    RequirementSyntax Requirement(CommandRequirementModel requirement) => new(_conditions.Convert(requirement.Condition)!, naming.ToStringLiteral(requirement.Message), SourceLocation.Start);

    ReadsSyntax Read(CommandReadModel read) => new(naming.ToDeclarationName(read.Name), naming.ToPropertyName(read.Key), SourceLocation.Start)
    {
        Alias = naming.ToPropertyName(read.Alias)
    };

    CommandStreamSyntax Route(CommandRouteModel route) => new(route.Source, route.Stream!, SourceLocation.Start)
    {
        StreamId = route.StreamId is null ? null : new PropertyMappingSyntax("streamId", new PathExpressionSyntax(naming.ToPropertyPath(route.StreamId), SourceLocation.Start), SourceLocation.Start)
    };

    CommandResponseSyntax? Response(CommandAuthoringModel authoring) => authoring.Response is { } source
        ? new ScalarCommandResponseSyntax(new PropertyResponseSourceSyntax(naming.ToPropertyName(source), SourceLocation.Start), SourceLocation.Start)
        : authoring.ResponseFields.Count > 0 ? new RecordCommandResponseSyntax(authoring.ResponseFields.Select(ResponseField), SourceLocation.Start) : null;

    ResponseFieldSyntax ResponseField(PropertyMappingModel field) => new(naming.ToPropertyName(field.Property), null, new PropertyResponseSourceSyntax(naming.ToPropertyName(((PropertyPathSource)field.Source).Path), SourceLocation.Start), SourceLocation.Start);

    ProducesSyntax Operation(OperationModel operation) => new(operation.Name, null, operation.Mappings.Select(Mapping).ToList(), SourceLocation.Start)
    {
        InlineOperation = new OperationSyntax(operation.Name, operation.System, operation.Inputs.Select(Input).ToList(), SourceLocation.Start)
        {
            Description = naming.ToStringLiteral(operation.Description),
            Execute = Phase(operation.SourceFilePath),
            Compensate = operation.Compensates ? Phase(operation.SourceFilePath) : null
        }
    };

    PropertyMappingSyntax Mapping(PropertyMappingModel mapping) => new(naming.ToPropertyName(mapping.Property), _sources.Convert(mapping.Source), SourceLocation.Start);

    PropertySyntax Input(PropertyModel input) => new(naming.ToPropertyName(input.Name), types.Convert(input.Type), SourceLocation.Start);

    OperationPhaseSyntax Phase(string? path) => new(null, path is null ? null : new FileReferenceSyntax(naming.ToFilePath(path)!, SourceLocation.Start), null, new ImplementationSyntax([], SourceLocation.Start), SourceLocation.Start);
}
