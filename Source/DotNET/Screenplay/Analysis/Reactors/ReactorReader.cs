// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reads the reactors a slice declares.
/// </summary>
/// <param name="compilation">The compilation being analyzed.</param>
/// <param name="paths">The <see cref="SourcePaths"/> rewriting the path of the file each reactor lives in.</param>
/// <remarks>
/// A reactor translates rather than automates when it turns what happened into something else that happens - by
/// returning further events, by executing a command, or by observing a sequence other than the event log. That is
/// read from the body, so a reactor that merely holds a command pipeline without using it is still an automation.
/// </remarks>
public class ReactorReader(Compilation compilation, SourcePaths paths)
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
    /// Reads a reactor.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <returns>The <see cref="ReactorModel"/>.</returns>
    public ReactorModel Read(INamedTypeSymbol type)
    {
        var handlers = Handlers(type).ToList();

        return new(
            type.Name,
            [.. handlers.Select(_ => _.Parameters[0].Type.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            IsTranslating(type, handlers),
            paths.Relative(type.SourceFilePath()));
    }

    /// <summary>
    /// Gets the methods dispatched to by event type.
    /// </summary>
    /// <param name="type">The type declaring the reactor.</param>
    /// <returns>The handlers, ordered so that the same reactor always reads the same way.</returns>
    static IEnumerable<IMethodSymbol> Handlers(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(_ => _ is { MethodKind: MethodKind.Ordinary, IsStatic: false, DeclaredAccessibility: Accessibility.Public } &&
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
            var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

            if (node.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(_ => IsPipelineExecution(_, semanticModel)))
            {
                return true;
            }
        }

        return false;
    }
}
