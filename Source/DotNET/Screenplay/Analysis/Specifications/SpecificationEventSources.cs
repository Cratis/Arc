// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Retains concrete occurrence sources and prevents unrelated computed sources from collapsing into one.
/// </summary>
internal class SpecificationEventSources
{
    readonly List<Source> _sources = [];
    readonly Dictionary<Compilation, HeldValues> _held = [];
    bool _commandScenario;
    Source? _commandSource;

    /// <summary>
    /// Gets the input identifier used to compare the issued command's occurrences.
    /// </summary>
    public string? CommandIdentifier { get; private set; }

    /// <summary>
    /// Gets whether the command scenario stated an explicit occurrence source.
    /// </summary>
    public bool HasExplicitCommandSources { get; private set; }

    /// <summary>
    /// Gets whether an explicit occurrence source could not be compared with the issued command's identity.
    /// </summary>
    public bool HasUnresolvedCommandSources { get; private set; }

    /// <summary>
    /// Reads the identity argument of the issued command before comparing its occurrences.
    /// </summary>
    /// <param name="command">The issued command type.</param>
    /// <param name="creation">The command construction.</param>
    /// <param name="model">The model resolving the construction.</param>
    /// <param name="models">The models resolving the command's identity contract.</param>
    public void ReadCommand(INamedTypeSymbol command, BaseObjectCreationExpressionSyntax creation, SemanticModel model, SemanticModels models)
    {
        _commandScenario = true;
        CommandIdentifier = new CommandIdentifierReader(models, new ScreenplayDiagnostics()).Read(command, command.Name);
        var constructor = model.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        var stated = SpecificationValues.Stated(creation, constructor)
            .Where(value => string.Equals(value.Name, CommandIdentifier, StringComparison.OrdinalIgnoreCase)).ToList();
        _commandSource = stated is [var value] ? SourceOf(value.Expression, model) : null;
    }

    /// <summary>
    /// Reads a source from an append, assertion, or fluent event-source builder.
    /// </summary>
    /// <param name="invocation">The call stating an occurrence.</param>
    /// <param name="method">The resolved method.</param>
    /// <param name="semanticModel">The model resolving the source expression.</param>
    /// <param name="draft">The scenario collecting the occurrences.</param>
    /// <returns>The concrete source, or null for a shared symbolic source.</returns>
    public LiteralSource? Read(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel semanticModel, SpecificationDraft draft)
    {
        var expression = CallArguments.For(invocation, method, "eventSourceId").SingleOrDefault();
        if (expression is null)
        {
            var builder = invocation.Expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(call => semanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol candidate &&
                    (candidate.ReturnType.Is(WellKnownTypeNames.EventSourceGivenBuilder) ||
                     candidate.ReturnType.Is(WellKnownTypeNames.CommandScenarioSourceGivenBuilder) ||
                     candidate.ReturnType.Is(WellKnownTypeNames.EventSourceWhenBuilder)));
            if (builder is not null && semanticModel.GetSymbolInfo(builder).Symbol is IMethodSymbol builderMethod)
            {
                expression = CallArguments.For(builder, builderMethod, "eventSourceId").SingleOrDefault();
            }
        }

        if (expression is null)
        {
            return null;
        }

        var source = SourceOf(expression, semanticModel);
        if (_commandScenario)
        {
            HasExplicitCommandSources = true;
            if (_commandSource is { } destination && Same(source, destination))
            {
                return null;
            }

            if (_commandSource is { } known &&
                ((source.Literal is not null && known.Literal is not null) || (source.Symbol is not null && known.Symbol is not null)))
            {
                if (source.Literal is null)
                {
                    draft.CannotRead("its distinct command event source cannot be stated as a concrete for value");
                }

                return source.Literal;
            }

            HasUnresolvedCommandSources = true;
            return null;
        }

        if (source.Unreadable is { } reason)
        {
            draft.CannotRead(reason);
        }

        if (_sources.Exists(previous => !(source.Literal is not null && previous.Literal is not null) && !Same(source, previous)))
        {
            draft.CannotRead("its event sources are not provably the same and cannot be stated as concrete for values");
        }

        _sources.Add(source);

        return source.Literal;
    }

    static bool Same(Source left, Source right) =>
        (left.Literal is not null && right.Literal is not null && SameLiteral(left.Literal, right.Literal)) ||
        (left.Symbol is not null && SymbolEqualityComparer.Default.Equals(left.Symbol, right.Symbol) &&
            SymbolEqualityComparer.Default.Equals(left.Receiver, right.Receiver));

    static bool SameLiteral(LiteralSource left, LiteralSource right) =>
        Equals(left, right) ||
        (left.Value is string leftText && right.Value is string rightText &&
            Guid.TryParse(leftText, out var leftId) && Guid.TryParse(rightText, out var rightId) && leftId == rightId);

    static LiteralSource? LiteralOf(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        expression = MappingSourceReader.Unwrap(expression);
        if (expression is BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument], Initializer: null } &&
            (semanticModel.GetTypeInfo(expression).Type.Is(WellKnownTypeNames.EventSourceId) ||
             semanticModel.GetTypeInfo(expression).Type?.FindBase(WellKnownTypeNames.ConceptAs) is not null))
        {
            return LiteralOf(argument.Expression, semanticModel);
        }

        var guidText = expression switch
        {
            BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [var text], Initializer: null }
                when semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol { Parameters: [var parameter] } constructor &&
                     constructor.ContainingType.Is("System.Guid") && parameter.Type.SpecialType == SpecialType.System_String => text.Expression,
            InvocationExpressionSyntax { ArgumentList.Arguments: [var text] }
                when semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol { IsStatic: true, Parameters: [var parameter] } method &&
                     string.Equals(method.Name, "Parse", StringComparison.Ordinal) && method.ContainingType.Is("System.Guid") && parameter.Type.SpecialType == SpecialType.System_String => text.Expression,
            _ => null
        };
        if (guidText is not null && semanticModel.GetConstantValue(guidText) is { HasValue: true, Value: string value } &&
            Guid.TryParse(value, out var guid))
        {
            return new(guid.ToString("D"));
        }

        var constant = semanticModel.GetConstantValue(expression);

        return constant is { HasValue: true, Value: string or bool or int or long or float or double or decimal }
            ? new(constant.Value)
            : null;
    }

    Source SourceOf(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        expression = MappingSourceReader.Unwrap(expression);
        var literal = LiteralOf(expression, semanticModel);
        var symbol = expression is IdentifierNameSyntax or MemberAccessExpressionSyntax
            ? semanticModel.GetSymbolInfo(expression).Symbol
            : null;
        if (!_held.TryGetValue(semanticModel.Compilation, out var held))
        {
            _held[semanticModel.Compilation] = held = new(new SemanticModels([semanticModel.Compilation]));
        }

        ISymbol? receiver = null;
        string? unreadable = null;
        if (symbol is { IsStatic: false } && expression is MemberAccessExpressionSyntax access &&
            MappingSourceReader.Unwrap(access.Expression) is not ThisExpressionSyntax)
        {
            var target = MappingSourceReader.Unwrap(access.Expression);
            receiver = target is IdentifierNameSyntax ? semanticModel.GetSymbolInfo(target).Symbol : null;
            if (receiver is null || !held.IsStable(receiver, semanticModel.Compilation))
            {
                unreadable = "its event source uses an instance receiver that is not provably stable";
                symbol = null;
            }
        }

        if (symbol is not (IFieldSymbol or ILocalSymbol or IPropertySymbol))
        {
            symbol = null;
        }
        else if (!held.IsStable(symbol, semanticModel.Compilation))
        {
            unreadable = $"its event source '{symbol.Name}' is reassigned or has a computed getter, so repeated references do not prove the same value";
            symbol = null;
        }

        return new(symbol, receiver, literal, unreadable);
    }

    sealed record Source(ISymbol? Symbol, ISymbol? Receiver, LiteralSource? Literal, string? Unreadable);
}
