// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reads the reactor handlers whose whole effect can be stated as a declarative reaction.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every handler is read through.</param>
/// <remarks>
/// A reaction states what a reactor sets off, and the reference evaluator runs it. That is only worth saying when it
/// is exactly what the reactor does at run time, so the shapes recognized here are the ones Chronicle and Arc give a
/// fixed meaning to:
/// <list type="bullet">
///   <item>An event returned by a synchronous handler declared to return that event, or a collection of events
///   returned as a sequence of events or of <see langword="object"/>, is appended to the event source of the triggering
///   event - what <c>produces</c> says when it names no destination.</item>
///   <item>A collection of commands returned as a sequence of <see langword="object"/>, or a command or collection of
///   commands returned through a <see cref="System.Threading.Tasks.Task{TResult}"/>, is executed through the command
///   pipeline with no caller, in order, stopping at the first that fails - what <c>invokes</c> says.</item>
/// </list>
/// The handler has to return one construction or one literal collection of constructions unconditionally, taking
/// nothing but the event and its context, and every value it gives them has to be a property of the triggering event,
/// the time it occurred, or a constant. Anything else is code, and the reaction keeps pointing at its file.
/// <para>
/// A reactor choosing the event source its events are appended to, executing its commands as the system, observing a
/// sequence other than the event log or filtered to an event source does something the reaction cannot say, so none
/// of its handlers are read.
/// </para>
/// </remarks>
public class ReactionReader(SemanticModels models)
{
    /// <summary>
    /// The property of the event context a reaction can read as <c>$context.occurred</c>.
    /// </summary>
    public const string OccurredProperty = "Occurred";

    /// <summary>
    /// The context path the occurrence time is read through.
    /// </summary>
    public const string OccurredContext = "occurred";

    const string TaskType = "System.Threading.Tasks.Task`1";
    const string FromResultMethod = "FromResult";

    /// <summary>
    /// Reads the declarative reactions of a reactor.
    /// </summary>
    /// <param name="reactor">The type declaring the reactor.</param>
    /// <param name="handlers">The handlers of the reactor.</param>
    /// <returns>The reactions, one per observed event whose handler can be stated.</returns>
    public IEnumerable<ReactionModel> Read(INamedTypeSymbol reactor, IReadOnlyList<IMethodSymbol> handlers)
    {
        if (reactor.FindInterface(WellKnownTypeNames.CanProvideEventSourceId) is not null)
        {
            return [];
        }

        var asSystem = ExecutesCommandsAsSystem(reactor);

        return handlers
            .GroupBy(_ => _.Parameters[0].Type, SymbolEqualityComparer.Default)
            .Where(_ => _.Count() == 1)
            .Select(_ => Read(_.Single(), asSystem))
            .OfType<ReactionModel>()
            .ToList();
    }

    /// <summary>
    /// Determines whether a reactor executes the commands it returns as the system.
    /// </summary>
    /// <param name="reactor">The type declaring the reactor.</param>
    /// <returns>True when it or a type it derives from says so.</returns>
    /// <remarks>
    /// Arc reads the attribute the way reflection does, inherited from a base type, so the bases are asked as well.
    /// </remarks>
    static bool ExecutesCommandsAsSystem(INamedTypeSymbol reactor)
    {
        for (var current = reactor; current is not null; current = current.BaseType)
        {
            if (current.HasAttribute(WellKnownTypeNames.ExecuteCommandsAsSystemAttribute))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Strips the parentheses around an expression, and nothing else.
    /// </summary>
    /// <param name="expression">The expression to strip.</param>
    /// <returns>The expression within.</returns>
    /// <remarks>
    /// A cast is a conversion, and a value converted on its way into a property is not the value of the property it
    /// came from, so unlike a mapping of a command a cast is not looked through.
    /// </remarks>
    static ExpressionSyntax Unparenthesize(ExpressionSyntax expression) =>
        expression is ParenthesizedExpressionSyntax parenthesized ? Unparenthesize(parenthesized.Expression) : expression;

    /// <summary>
    /// Gets the single expression a body returns.
    /// </summary>
    /// <param name="body">The body of the handler.</param>
    /// <returns>The expression, or <see langword="null"/> when the body does anything else.</returns>
    static ExpressionSyntax? ReturnedBy(SyntaxNode body) => body switch
    {
        ExpressionSyntax expression => expression,
        BlockSyntax { Statements: [ReturnStatementSyntax { Expression: { } expression }] } => expression,
        _ => null
    };

    /// <summary>
    /// Gets the element type of a sequence, ignoring text.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The element type, or <see langword="null"/> when the type is not a sequence.</returns>
    static ITypeSymbol? ElementOf(ITypeSymbol type) => EventReader.IsEvent(type) ? null : CollectionElements.ElementOf(type);

    /// <summary>
    /// Gets the items of a literal collection.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <returns>The items, or <see langword="null"/> when the expression is not a literal collection.</returns>
    static IReadOnlyList<ExpressionSyntax>? ItemsOf(ExpressionSyntax expression) => Unparenthesize(expression) switch
    {
        CollectionExpressionSyntax collection when collection.Elements.All(_ => _ is ExpressionElementSyntax) =>
            [.. collection.Elements.OfType<ExpressionElementSyntax>().Select(_ => _.Expression)],
        ArrayCreationExpressionSyntax { Initializer: { } initializer } => [.. initializer.Expressions],
        ImplicitArrayCreationExpressionSyntax { Initializer: { } initializer } => [.. initializer.Expressions],
        _ => null
    };

    /// <summary>
    /// Gets the value a handler hands back, looking through a completed task.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="returned">The expression the body returns.</param>
    /// <param name="semanticModel">The model the body is read through.</param>
    /// <param name="result">The type of the value handed back.</param>
    /// <param name="asynchronous">Whether the value is handed back through a task.</param>
    /// <returns>The expression of the value, or <see langword="null"/> when it is not handed back directly.</returns>
    static ExpressionSyntax? ValueOf(
        IMethodSymbol handler,
        ExpressionSyntax returned,
        SemanticModel semanticModel,
        out ITypeSymbol? result,
        out bool asynchronous)
    {
        result = handler.ReturnType;
        asynchronous = false;
        if (handler.ReturnType is not INamedTypeSymbol { TypeArguments: [var awaited] } task ||
            task.OriginalDefinition.FullMetadataName() != TaskType)
        {
            return returned;
        }

        result = awaited;
        asynchronous = true;
        if (handler.IsAsync)
        {
            return returned;
        }

        return Unparenthesize(returned) is InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] } invocation &&
            semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: true } method &&
            string.Equals(method.Name, FromResultMethod, StringComparison.Ordinal) &&
            method.ContainingType.FullMetadataName() == "System.Threading.Tasks.Task"
                ? argument.Expression
                : null;
    }

    /// <summary>
    /// Determines whether a returned value is one Chronicle hands to a side effect handler as it is.
    /// </summary>
    /// <param name="result">The type the handler hands back.</param>
    /// <param name="asynchronous">Whether it is handed back through a task.</param>
    /// <param name="created">The types the handler constructs.</param>
    /// <param name="sequence">Whether the value is a sequence.</param>
    /// <returns>True when the handler is a handler Chronicle registers and the value has one fixed meaning.</returns>
    /// <remarks>
    /// Chronicle registers a synchronous handler only when it returns an event type or a sequence of events or of
    /// objects; a synchronous handler returning a command is rejected. Whatever a task completes with is handed to the
    /// side effect handlers, and a value they do not all agree on - events mixed with commands - fails at run time.
    /// </remarks>
    static bool IsHandedOver(ITypeSymbol result, bool asynchronous, IReadOnlyList<INamedTypeSymbol> created, bool sequence)
    {
        var events = created.All(EventReader.IsEvent);
        var commands = created.All(CommandReader.IsCommand);
        if (!events && !commands)
        {
            return false;
        }

        if (asynchronous)
        {
            return true;
        }

        if (!sequence)
        {
            return events && SymbolEqualityComparer.Default.Equals(result, created[0]);
        }

        var element = ElementOf(result);

        return element?.SpecialType == SpecialType.System_Object || (events && element is not null && EventReader.IsEvent(element));
    }

    /// <summary>
    /// Reads one handler.
    /// </summary>
    /// <param name="handler">The handler to read.</param>
    /// <param name="asSystem">Whether the reactor executes the commands it returns as the system.</param>
    /// <returns>The <see cref="ReactionModel"/>, or <see langword="null"/> when the handler is code.</returns>
    ReactionModel? Read(IMethodSymbol handler, bool asSystem)
    {
        if (handler.IsGenericMethod ||
            handler.ReturnsVoid ||
            handler.HasAttribute(WellKnownTypeNames.ReplayAttribute) ||
            handler.Parameters.Skip(1).Any(_ => !_.Type.Is(WellKnownTypeNames.EventContext)) ||
            handler.Parameters.Length > 2 ||
            HandlerBodies.Of(handler).ToList() is not [var body] ||
            models.For(body.SyntaxTree) is not { } semanticModel ||
            ReturnedBy(body) is not { } returned ||
            ValueOf(handler, returned, semanticModel, out var result, out var asynchronous) is not { } value ||
            result is null)
        {
            return null;
        }

        var sequence = ElementOf(result) is not null;
        var items = sequence ? ItemsOf(value) : [value];
        if (items is null || items.Count == 0)
        {
            return null;
        }

        var creations = new List<(BaseObjectCreationExpressionSyntax Creation, INamedTypeSymbol Type)>();
        foreach (var item in items)
        {
            if (Unparenthesize(item) is not BaseObjectCreationExpressionSyntax creation ||
                semanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type ||
                !InlineProductionShape.IsSupported(creation, semanticModel))
            {
                return null;
            }

            creations.Add((creation, type));
        }

        if (!IsHandedOver(result, asynchronous, [.. creations.Select(_ => _.Type)], sequence) ||
            (asSystem && CommandReader.IsCommand(creations[0].Type)))
        {
            return null;
        }

        var trigger = handler.Parameters[0];
        var context = handler.Parameters.Length > 1 ? handler.Parameters[1] : null;
        var produces = new List<ProducesModel>();
        var invokes = new List<InvocationModel>();
        foreach (var (creation, type) in creations)
        {
            if (ReactionMappings.Read(creation, semanticModel, type, trigger, context) is not { } mappings)
            {
                return null;
            }

            if (EventReader.IsEvent(type))
            {
                produces.Add(new(type.Name, null, mappings) { EventTypeIdentity = EventProducers.IdentityOf(type) });
            }
            else
            {
                invokes.Add(new(type.Name, mappings));
            }
        }

        return new(trigger.Type.Name, produces, invokes);
    }
}
