// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reads the reactors a slice declares.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every handler is read through.</param>
/// <param name="paths">The <see cref="SourcePaths"/> rewriting the path of the file each reactor lives in.</param>
/// <remarks>
/// A reactor translates rather than automates when it turns what happened into something else that happens - by
/// returning further events, by executing a command, or by observing a sequence other than the event log. That is
/// read from the body, so a reactor that merely holds a command pipeline without using it is still an automation.
/// <para>
/// A handler inherited from a base a project below it declares is written wherever that base is, so which model
/// reads a body is asked rather than assumed.
/// </para>
/// </remarks>
public class ReactorReader(SemanticModels models, SourcePaths paths)
{
    /// <summary>
    /// The name of the argument naming the sequence a reactor observes.
    /// </summary>
    public const string EventSequenceArgument = "EventSequenceId";

    /// <summary>
    /// The identifier of the sequence a reactor observes unless it says otherwise.
    /// </summary>
    public const string EventLogSequence = "event-log";

    /// <summary>
    /// The method a command pipeline is executed through.
    /// </summary>
    public const string ExecuteMethod = "Execute";

    /// <summary>
    /// Determines whether a type is a reactor.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a reactor.</returns>
    public static bool IsReactor(ITypeSymbol type) =>
        type is { IsAbstract: false, TypeKind: TypeKind.Class } && type.FindInterface(WellKnownTypeNames.Reactor) is not null;

    /// <summary>
    /// Determines whether a reactor observes the event log, which is what a reaction to an event is set off by.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <returns>True when the reactor names no sequence or names the event log.</returns>
    public static bool ObservesTheEventLog(INamedTypeSymbol type) =>
        SequenceOf(type) is not { Length: > 0 } sequence || string.Equals(sequence, EventLogSequence, StringComparison.Ordinal);

    /// <summary>
    /// Gets the declarative reactions of a reactor.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <param name="models">The <see cref="SemanticModels"/> every handler is read through.</param>
    /// <returns>The reactions, empty when the type is not a reactor or none of its handlers can be stated.</returns>
    public static IEnumerable<ReactionModel> ReactionsOf(INamedTypeSymbol type, SemanticModels models) =>
        IsReactor(type) ? ReactionsOf(type, [.. Handlers(type)], EventSourceReader.ReadObserved(type), models) : [];

    /// <summary>
    /// Gets the names of the events a reactor observes.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <returns>The names of the observed events.</returns>
    public static IEnumerable<string> ObservedBy(INamedTypeSymbol type) =>
        Handlers(type).Select(_ => _.Parameters[0].Type.Name).Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Reads a reactor.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <returns>The <see cref="ReactorModel"/>.</returns>
    public ReactorModel Read(INamedTypeSymbol type)
    {
        var handlers = Handlers(type).ToList();
        var eventSource = EventSourceReader.ReadObserved(type);

        return new(
            type.Name,
            [.. handlers.Select(_ => _.Parameters[0].Type.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            IsTranslating(type, handlers),
            paths.Relative(type.SourceFilePath()),
            eventSource)
        {
            Reactions = ReactionsOf(type, handlers, eventSource, models)
        };
    }

    /// <summary>
    /// Gets the declarative reactions of a reactor whose handlers are known.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <param name="handlers">The handlers of the reactor.</param>
    /// <param name="eventSource">The event source the reactor is filtered to, if any.</param>
    /// <param name="models">The <see cref="SemanticModels"/> every handler is read through.</param>
    /// <returns>The reactions.</returns>
    static IEnumerable<ReactionModel> ReactionsOf(
        INamedTypeSymbol type,
        IReadOnlyList<IMethodSymbol> handlers,
        ObservedEventSourceModel? eventSource,
        SemanticModels models) =>
        eventSource is null && ObservesTheEventLog(type) ? new ReactionReader(models).Read(type, handlers) : [];

    /// <summary>
    /// Gets the methods dispatched to by event type.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <returns>The handlers, ordered so that the same reactor always reads the same way.</returns>
    /// <remarks>
    /// Accessibility says nothing about whether a method handles an event. Chronicle dispatches to every instance
    /// method whose first parameter is an event type, public or not, so a reactor whose handlers are private observes
    /// those events at runtime and the document has to say so.
    /// </remarks>
    static IEnumerable<IMethodSymbol> Handlers(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(_ => _ is { MethodKind: MethodKind.Ordinary, IsStatic: false } &&
                _.Parameters.Length > 0 && EventReader.IsEvent(_.Parameters[0].Type))
            .OrderBy(_ => _.ToDisplayString(), StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a reactor produces further events rather than only causing an effect.
    /// </summary>
    /// <param name="handler">The handler to check.</param>
    /// <returns>True when the handler returns something.</returns>
    static bool ProducesFurtherEvents(IMethodSymbol handler)
    {
        if (handler.ReturnsVoid)
        {
            return false;
        }

        return handler.ReturnType is not INamedTypeSymbol { TypeArguments.Length: 0, Name: "Task" or "ValueTask" };
    }

    /// <summary>
    /// Gets the identifier of the sequence a reactor observes.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <returns>The identifier, or <see langword="null"/> when the reactor does not name one.</returns>
    static string? SequenceOf(INamedTypeSymbol type)
    {
        var attribute = type.GetAttribute(WellKnownTypeNames.ReactorAttribute);

        return (attribute?.GetNamedArgument(EventSequenceArgument) ?? attribute?.GetArgument(1)) as string;
    }

    /// <summary>
    /// Determines whether an invocation executes a command through a pipeline.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <param name="semanticModel">The semantic model of the tree the invocation lives in.</param>
    /// <returns>True when the invocation is a command execution.</returns>
    static bool IsPipelineExecution(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        string.Equals(method.Name, ExecuteMethod, StringComparison.Ordinal) &&
        (method.ContainingType.Is(WellKnownTypeNames.CommandPipeline) ||
            method.ContainingType.FindInterface(WellKnownTypeNames.CommandPipeline) is not null);

    /// <summary>
    /// Determines whether a reactor turns what happened into something else that happens.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <param name="handlers">The handlers of the reactor.</param>
    /// <returns>True when the reactor translates.</returns>
    bool IsTranslating(INamedTypeSymbol type, IReadOnlyList<IMethodSymbol> handlers)
    {
        var sequence = SequenceOf(type);
        if (!string.IsNullOrEmpty(sequence) && !string.Equals(sequence, EventLogSequence, StringComparison.Ordinal))
        {
            return true;
        }

        return handlers.Any(ProducesFurtherEvents) || handlers.Any(ExecutesACommand);
    }

    /// <summary>
    /// Determines whether a handler executes a command.
    /// </summary>
    /// <param name="handler">The handler to check.</param>
    /// <returns>True when the body calls into a command pipeline.</returns>
    bool ExecutesACommand(IMethodSymbol handler)
    {
        foreach (var reference in handler.DeclaringSyntaxReferences)
        {
            var node = reference.GetSyntax();
            if (models.For(node.SyntaxTree) is not { } semanticModel)
            {
                continue;
            }

            if (node.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(_ => IsPipelineExecution(_, semanticModel)))
            {
                return true;
            }
        }

        return false;
    }
}
