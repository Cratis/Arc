// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Reactors;

/// <summary>
/// Decides which reactor handlers the document states as declarative reactions.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="application">The application the document is written for.</param>
/// <remarks>
/// Analysis says which handlers are nothing but what they return. Whether that can be written depends on the document
/// as well: a reaction producing an event or invoking a command the document does not declare exactly once, giving a
/// value to a property the document does not declare, or leaving out a value the command requires does not bind. Such
/// a handler keeps pointing at its file, as it did before it could be stated at all. The same decision is asked by the
/// reactions and by the specifications reaching them, so the two can never disagree about what runs.
/// </remarks>
public class DeclarativeReactions(IScreenplayNaming naming, ApplicationModel application)
{
    /// <summary>
    /// The words a line beneath <c>produces</c> or <c>invokes</c> reads as a directive rather than as a mapping.
    /// </summary>
    static readonly HashSet<string> _reserved = new(StringComparer.Ordinal) { "for", "tag", "when" };

    /// <summary>
    /// Gets every declarative reaction the document states, with the reactor stating it.
    /// </summary>
    /// <returns>The reactions.</returns>
    public IEnumerable<(ReactorModel Reactor, ReactionModel Reaction)> All() =>
        application.Slices
            .SelectMany(_ => _.Reactors)
            .SelectMany(reactor => reactor.ObservedEvents
                .Select(@event => For(reactor, @event))
                .OfType<ReactionModel>()
                .Select(reaction => (reactor, reaction)));

    /// <summary>
    /// Gets the declarative reaction a reactor states for one observed event.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <param name="event">The name of the observed event.</param>
    /// <returns>The <see cref="ReactionModel"/>, or <see langword="null"/> when the handler stays a file reference.</returns>
    public ReactionModel? For(ReactorModel reactor, string @event)
    {
        var name = naming.ToDeclarationName(@event);
        var reactions = reactor.Reactions.Where(_ => naming.ToDeclarationName(_.EventName) == name).ToList();
        if (reactor.EventSource is not null || reactions is not [var reaction])
        {
            return null;
        }

        return reaction.Produces.All(CanState) && reaction.Invokes.All(CanState) ? reaction : null;
    }

    /// <summary>
    /// Gets the names of every event a reactor of the document observes, whether stated declaratively or not.
    /// </summary>
    /// <returns>The declaration names of the observed events.</returns>
    public IReadOnlySet<string> Observed() =>
        application.Slices.SelectMany(_ => _.Reactors).SelectMany(_ => _.ObservedEvents).Select(naming.ToDeclarationName).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a value is written into the document exactly as the source states it.
    /// </summary>
    /// <param name="source">The source of the value.</param>
    /// <returns>True unless it is text the document would write differently.</returns>
    /// <remarks>
    /// Text is put on one line, trimmed and has its double quotes replaced on its way into the document, which is
    /// harmless for a description and wrong for a value a reaction gives an event. Such a value, and text the
    /// document would read back as an escape, keeps the handler a file reference.
    /// </remarks>
    bool RoundTrips(MappingSourceModel source) =>
        source is not LiteralSource { Value: string text } ||
        (string.Equals(naming.ToStringLiteral(text), text, StringComparison.Ordinal) && !text.Contains('\\', StringComparison.Ordinal));

    /// <summary>
    /// Determines whether a production binds against the events the document declares.
    /// </summary>
    /// <param name="produces">The production.</param>
    /// <returns>True when it can be stated.</returns>
    bool CanState(ProducesModel produces)
    {
        var name = naming.ToDeclarationName(produces.EventName);
        var events = application.Slices.SelectMany(_ => _.Events).Where(_ => naming.ToDeclarationName(_.Name) == name).ToList();
        if (events.Count == 0 || events.Select(_ => string.Join(',', _.Properties.Select(property => property.Name))).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            return false;
        }

        var declared = events[0].Properties.Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);

        return produces.Mappings.All(_ => declared.Contains(_.Property) && !_reserved.Contains(naming.ToPropertyName(_.Property)) && RoundTrips(_.Source));
    }

    /// <summary>
    /// Determines whether an invocation binds against the commands the document declares.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>True when it can be stated.</returns>
    bool CanState(InvocationModel invocation)
    {
        var name = naming.ToDeclarationName(invocation.CommandName);
        if (application.Slices.SelectMany(_ => _.Commands).Where(_ => naming.ToDeclarationName(_.Name) == name).ToList() is not [var command])
        {
            return false;
        }

        var declared = command.Properties.Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);
        var mapped = invocation.Mappings.Select(_ => _.Property).ToHashSet(StringComparer.Ordinal);

        return declared.SetEquals(mapped) &&
            !(command.Authoring?.Generated ?? []).Any(_ => mapped.Contains(_.Name)) &&
            !mapped.Any(_ => _reserved.Contains(naming.ToPropertyName(_))) &&
            invocation.Mappings.All(_ => RoundTrips(_.Source));
    }
}
