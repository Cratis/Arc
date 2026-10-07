// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using ScreenplaySliceType = Cratis.Screenplay.Syntax.SliceType;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Visits a Screenplay slice and produces the slice the board draws - its events, the command it handles,
/// the read model it builds and the queries it answers.
/// </summary>
/// <param name="documentId">The identifier of the document being read.</param>
/// <param name="path">The path to the feature holding the slice.</param>
/// <param name="owners">The events declared across the application.</param>
/// <param name="warnings">Where to report everything the board cannot hold.</param>
public class SliceSyntaxVisitor(string documentId, string path, ScreenplayEventOwners owners, EventModelWarnings warnings)
    : ISliceSyntaxVisitor<Slice>
{
    int _sortOrder;

    /// <inheritdoc/>
    public Slice Visit(SliceSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        var slicePath = $"{path}/{syntax.Name}";
        var sliceType = ToBoard(syntax.Type);
        var isStateView = sliceType == SliceType.StateView;
        var command = isStateView ? null : Command(syntax, slicePath);
        var readModel = isStateView ? ReadModel(syntax, slicePath) : null;

        Warn(syntax, slicePath, isStateView);

        return new Slice(
            DeterministicId.From(documentId, slicePath, "slice"),
            syntax.Name,
            sliceType,
            syntax.Description ?? string.Empty,
            SliceStatus.Done,
            Collapsed: false,
            SortOrder: _sortOrder++,
            command,
            readModel,
            ExternalEvents: [],
            Events: Events(syntax, slicePath, sliceType),
            Queries: Queries(syntax, slicePath),
            Actors: [],
            Specifications: [],
            CommentCount: 0);
    }

    static SliceType ToBoard(ScreenplaySliceType type) => type switch
    {
        ScreenplaySliceType.StateView => SliceType.StateView,
        ScreenplaySliceType.Automation => SliceType.Automation,
        ScreenplaySliceType.Translate => SliceType.Translator,
        _ => SliceType.StateChange
    };

    static IReadOnlyList<CommandPropertyRules> Rules(CommandSyntax command) =>
    [
        .. (command.Validations ?? [])
            .OfType<DeclarativeValidateSyntax>()
            .SelectMany(validation => validation.Rules ?? [])
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Property))
            .GroupBy(rule => rule.Property, StringComparer.Ordinal)
            .Select(group => new CommandPropertyRules(
                group.Key,
                [.. group.Select(rule => new CommandRule(rule.Message ?? string.Empty, RuleType(rule.Rule)))]))
    ];

    static string TagText(TagSyntax tag) => tag.Value switch
    {
        LiteralExpressionSyntax literal => literal.Value?.ToString() ?? string.Empty,
        PathExpressionSyntax path => path.Path,
        ContextExpressionSyntax context => $"$context.{context.Path}",
        EnvironmentExpressionSyntax environment => $"$env.{environment.Name}",
        _ => string.Empty
    };

    static string RuleType(ValidationRuleKind kind)
    {
        var name = kind.ToString();
        return string.Concat(char.ToLowerInvariant(name[0]), name[1..]);
    }

    static EventConstraints? Constraints(SliceSyntax syntax, string eventName)
    {
        var constraints = (syntax.Constraints ?? []).ToList();
        var unique = constraints
            .OfType<UniquePropertyConstraintSyntax>()
            .FirstOrDefault(constraint => string.Equals(constraint.Event, eventName, StringComparison.OrdinalIgnoreCase));
        var uniqueEventType = constraints
            .OfType<UniqueEventConstraintSyntax>()
            .FirstOrDefault(constraint => string.Equals(constraint.Event, eventName, StringComparison.OrdinalIgnoreCase));

        return unique is null && uniqueEventType is null
            ? null
            : new EventConstraints(
                unique is null ? null : new EventConstraint(unique.Property ?? string.Empty, unique.Name ?? string.Empty),
                uniqueEventType is null ? null : new EventConstraint(uniqueEventType.Event ?? string.Empty, uniqueEventType.Name ?? string.Empty));
    }

    static string? ReadModelName(SliceSyntax syntax) =>
        (syntax.Projections ?? []).FirstOrDefault()?.ReadModel ??
        (syntax.Queries ?? []).FirstOrDefault()?.ReturnType?.Name ??
        (syntax.ReadModels ?? []).FirstOrDefault()?.Name;

    CommandItem? Command(SliceSyntax syntax, string slicePath)
    {
        if ((syntax.Commands ?? []).FirstOrDefault() is not { } command)
        {
            return null;
        }

        return new CommandItem(
            DeterministicId.From(documentId, slicePath, "command", command.Name),
            command.Name,
            command.Properties.Where(property => !property.IsGenerated).ToSchema(),
            SchemaSynthesizer.EmptyObjectSchema(),
            command.Description ?? string.Empty,
            Rules(command));
    }

    ReadModelItem? ReadModel(SliceSyntax syntax, string slicePath)
    {
        var name = ReadModelName(syntax);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var declaration =
            (syntax.ReadModels ?? []).FirstOrDefault(readModel => string.Equals(readModel.Name, name, StringComparison.OrdinalIgnoreCase)) ??
            (syntax.ReadModels ?? []).FirstOrDefault();

        return new ReadModelItem(
            DeterministicId.From(documentId, slicePath, "readModel", name),
            name,
            declaration?.Properties.ToSchema() ?? SchemaSynthesizer.EmptyObjectSchema(),
            Materializes: (syntax.Projections ?? []).Any());
    }

    List<EventItem> Events(SliceSyntax syntax, string slicePath, SliceType sliceType)
    {
        var declared = EventDeclarations.In(syntax)
            .Where(@event => !string.IsNullOrWhiteSpace(@event.Name))
            .Select(@event => new EventItem(
                owners.IdentityFor(@event.Name) ?? DeterministicId.From(documentId, slicePath, "event", @event.Name),
                @event.Name,
                @event.Properties.ToSchema(),
                SourceEventId: null,
                Tags: [.. (@event.Tags ?? []).Select(TagText)],
                Constraints: Constraints(syntax, @event.Name)))
            .ToList();

        if (sliceType == SliceType.StateChange)
        {
            var producedDeclaredNames = declared.Select(_ => _.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var produced = (syntax.Commands ?? []).SelectMany(_ => _.Produces ?? [])
                .Where(_ => !producedDeclaredNames.Contains(_.Event))
                .GroupBy(_ => _.Event, StringComparer.OrdinalIgnoreCase)
                .Select(group => new EventItem(
                    DeterministicId.From(documentId, slicePath, "produces", group.Key),
                    group.Key,
                    owners.SchemaFor(group.Key),
                    SourceEventId: null,
                    Tags: [],
                    Constraints: null));
            return [.. declared, .. produced];
        }

        // Every other kind of slice also observes what it reads, carrying the shape the producing slice
        // declares so the board has something to draw the flow into this slice from.
        var declaredNames = declared.Select(@event => @event.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var consumed = syntax.ConsumedEventNames()
            .Where(name => !declaredNames.Contains(name))
            .Select(name => new EventItem(
                DeterministicId.From(documentId, slicePath, "consumes", name),
                name,
                owners.SchemaFor(name),
                owners.IdentityFor(name)?.ToString(),
                Tags: [],
                Constraints: null));

        return [.. declared, .. consumed];
    }

    IReadOnlyList<QueryItem> Queries(SliceSyntax syntax, string slicePath) =>
    [
        .. (syntax.Queries ?? []).Select(query => new QueryItem(
            DeterministicId.From(documentId, slicePath, "query", query.Name),
            query.Name,
            [
                .. new[] { query.By }
                    .Concat(query.Filters ?? [])
                    .OfType<QueryParameterSyntax>()
                    .Select(parameter => new QueryParameter(parameter.Name, parameter.Type.ToQueryParameterType()))
            ]))
    ];

    void Warn(SliceSyntax syntax, string slicePath, bool isStateView)
    {
        var commands = (syntax.Commands ?? []).ToList();
        var projections = (syntax.Projections ?? []).ToList();
        var readModels = (syntax.ReadModels ?? []).ToList();
        var carriedReadModel = ReadModelName(syntax);
        var droppedCommands = isStateView ? commands.Count : Math.Max(commands.Count - 1, 0);
        var droppedCommandsBecause = isStateView
            ? "the board holds a command on a slice that changes state, and this one views state"
            : "the board holds one command per slice";

        warnings.Dropped(
            EventModelWarningCodes.SliceConstructNotCarried,
            slicePath,
            droppedCommands,
            "command",
            droppedCommandsBecause,
            syntax.Location);

        warnings.Dropped(
            EventModelWarningCodes.SliceConstructNotCarried,
            slicePath,
            Math.Max(projections.Count - 1, 0),
            "further projection",
            "the board holds one read model per slice, built by the first projection declared",
            syntax.Location);

        warnings.Dropped(
            EventModelWarningCodes.SliceConstructNotCarried,
            slicePath,
            readModels.Count - (readModels.Exists(readModel => string.Equals(readModel.Name, carriedReadModel, StringComparison.Ordinal)) ? 1 : 0),
            "readmodel declaration",
            "the board names a read model after the projection that builds it",
            syntax.Location);

        warnings.Present(
            EventModelWarningCodes.ReadModelNotCarried,
            !isStateView && (projections.Count > 0 || (syntax.Queries ?? []).Any()),
            slicePath,
            "the board only carries a read model on a slice that views state, so the one this slice builds was not added",
            syntax.Location);

        warnings.Dropped(EventModelWarningCodes.SliceConstructNotCarried, slicePath, (syntax.Reactions ?? []).Count(), "reaction", "the events it triggers on are referenced by the slice, but nothing records the reaction itself", syntax.Location);
        warnings.Dropped(EventModelWarningCodes.SliceConstructNotCarried, slicePath, (syntax.Screens ?? []).Count(), "screen", "the board has no way to record a screen", syntax.Location);
        warnings.Dropped(EventModelWarningCodes.SliceConstructNotCarried, slicePath, (syntax.Captures ?? []).Count(), "capture", "the board has no way to record a capture", syntax.Location);
        warnings.Dropped(EventModelWarningCodes.SliceConstructNotCarried, slicePath, (syntax.Reducers ?? []).Count(), "reducer", "the board builds a read model from a projection", syntax.Location);
        warnings.Dropped(EventModelWarningCodes.SliceConstructNotCarried, slicePath, (syntax.Specifications ?? []).Count(), "specification", "the board holds a specification against the identities of the items it exercises, which this document does not yet state", syntax.Location);
        warnings.Dropped(EventModelWarningCodes.SliceConstructNotCarried, slicePath, (syntax.Constraints ?? []).OfType<FileConstraintSyntax>().Count(), "file-backed constraint", "it points at code outside the document, and the board records a constraint as a declaration on an event", syntax.Location);

        foreach (var command in commands)
        {
            WarnForCommand(slicePath, command);
        }

        foreach (var query in syntax.Queries ?? [])
        {
            WarnForQuery(slicePath, query);
        }
    }

    void WarnForCommand(string slicePath, CommandSyntax command)
    {
        var validations = (command.Validations ?? []).ToList();

        warnings.Present(
            EventModelWarningCodes.CommandConstructNotCarried,
            (command.Produces ?? []).Any(_ => (_.Mappings ?? []).Any() || _.When is not null),
            slicePath,
            $"the property mappings and conditions for events produced by command '{command.Name}' are not displayed by the board",
            command.Location);
        warnings.Present(EventModelWarningCodes.CommandConstructNotCarried, command.Authorize is not null, slicePath, $"the policies authorizing the command '{command.Name}' are not added to the board", command.Location);
        warnings.Present(EventModelWarningCodes.CommandConstructNotCarried, command.Concurrency is not null, slicePath, $"the concurrency scope of the command '{command.Name}' is not added to the board", command.Location);
        warnings.Present(EventModelWarningCodes.CommandConstructNotCarried, validations.OfType<CodeValidateSyntax>().Any(), slicePath, $"the code validating the command '{command.Name}' is not added to the board, which records validation as a fixed set of property rules", command.Location);
        warnings.Present(EventModelWarningCodes.CommandConstructNotCarried, (command.Reads ?? []).Any(), slicePath, $"the read models the command '{command.Name}' reads are not added to the board, so the state it decides against is no longer stated", command.Location);
        var requirements = validations.OfType<DeclarativeValidateSyntax>().SelectMany(validation => validation.Requirements ?? []).Any();
        warnings.Present(
            EventModelWarningCodes.CommandConstructNotCarried,
            requirements,
            slicePath,
            $"the requirements the command '{command.Name}' states are not added to the board, which records validation per property",
            command.Location);
    }

    void WarnForQuery(string slicePath, QuerySyntax query)
    {
        warnings.Present(EventModelWarningCodes.QueryConstructNotCarried, query.Scope is not null, slicePath, $"the scope the query '{query.Name}' is declared with is not added to the board", query.Location);
        warnings.Present(EventModelWarningCodes.QueryConstructNotCarried, query.IsObservable, slicePath, $"the query '{query.Name}' is declared observable, and the board records a read model without recording whether a read against it is live", query.Location);
        warnings.Present(EventModelWarningCodes.QueryConstructNotCarried, query.Authorize is not null, slicePath, $"the policies authorizing the query '{query.Name}' are not added to the board", query.Location);
    }
}
