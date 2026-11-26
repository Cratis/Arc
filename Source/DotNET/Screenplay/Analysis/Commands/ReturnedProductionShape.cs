// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves returned effects through the pipeline's supported return envelopes, not arbitrary constructions.
/// </summary>
internal static class ReturnedProductionShape
{
    /// <summary>
    /// Checks whether the expression reaches a handler return without helpers, aliases, spreads or deferred execution.
    /// </summary>
    /// <param name="expression">The returned event or destination wrapper.</param>
    /// <param name="body">The handler body.</param>
    /// <param name="model">The model binding envelopes.</param>
    /// <returns>Whether return reachability is proven.</returns>
    internal static bool Reaches(ExpressionSyntax expression, SyntaxNode body, SemanticModel model) =>
        !expression.Ancestors().TakeWhile(node => node != body).Any(node => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax) &&
        (ReturnedEventList.Carries(expression, body, model) || ReachesInline(expression, body, model));

    /// <summary>Finds an inline tuple whose entire effect set reaches the returned expression.</summary>
    /// <param name="returned">The handler's directly returned expression.</param>
    /// <param name="model">The model binding its supported return envelopes.</param>
    /// <returns>The proven tuple, or null for non-tuple or unsupported returns.</returns>
    internal static TupleExpressionSyntax? TupleOf(ExpressionSyntax returned, SemanticModel model) =>
        returned.DescendantNodesAndSelf().OfType<TupleExpressionSyntax>()
            .FirstOrDefault(tuple => Reaches(tuple, returned, model));

    /// <summary>
    /// Checks the exact Chronicle identity types that update tuple command context.
    /// </summary>
    /// <param name="type">The tuple element type.</param>
    /// <returns>Whether it is a Chronicle source identity.</returns>
    internal static bool IsIdentity(ITypeSymbol? type) => type is not null &&
        (type.Is("Cratis.Chronicle.Events.EventSourceId") || type.Is("Cratis.Chronicle.Events.EventSourceId`1") || type.FindBase("Cratis.Chronicle.Events.EventSourceId`1") is not null);

    /// <summary>
    /// Reads the identity element after the handler's contextual tuple conversions, never from a raw response value.
    /// </summary>
    /// <param name="tuple">The returned tuple.</param>
    /// <param name="model">The semantic model binding its target type.</param>
    /// <returns>The exact identity argument, or null.</returns>
    internal static ExpressionSyntax? IdentityOf(TupleExpressionSyntax tuple, SemanticModel model)
    {
        var identities = TupleType(tuple, model)?.TupleElements.Select((element, index) => (element.Type, Index: index))
            .Where(element => IsIdentity(element.Type)).ToArray();

        return identities is [var identity] && identity.Index < tuple.Arguments.Count ? tuple.Arguments[identity.Index].Expression : null;
    }

    /// <summary>
    /// Checks whether a returned tuple contains only effects or a response proven not to override identity.
    /// </summary>
    /// <param name="tuple">The returned tuple.</param>
    /// <param name="model">The semantic model binding its target type.</param>
    /// <returns>Whether its plain events retain command context.</returns>
    internal static bool UsesCommandContext(TupleExpressionSyntax tuple, SemanticModel model) =>
        IdentityOf(tuple, model) is null && SupportedTuple(tuple, model);

    /// <summary>
    /// Classifies effect slots; object-array contents must also pass the inline effect proof.
    /// </summary>
    /// <param name="type">The contextual slot type.</param>
    /// <returns>Whether the slot can contain returned effects.</returns>
    internal static bool IsEffect(ITypeSymbol? type) => type is not null &&
        (EventReader.IsEvent(type) || IsEnvelope(type) || IsSequenceEffect(type));

    /// <summary>Identifies a OneOf envelope after its return shape has been proved.</summary>
    /// <param name="expression">The returned event wrapper.</param>
    /// <param name="model">The semantic model of the handler.</param>
    /// <returns>Whether a surrounding expression carries the OneOf result.</returns>
    internal static bool WithinOneOf(ExpressionSyntax expression, SemanticModel model) =>
        expression.AncestorsAndSelf().OfType<ExpressionSyntax>().Any(value => model.GetTypeInfo(value).ConvertedType?.Is("OneOf.OneOf`2") == true);

    static bool ReachesInline(ExpressionSyntax expression, SyntaxNode body, SemanticModel model)
    {
        for (SyntaxNode? node = expression; node is not null; node = node.Parent)
        {
            if (node is ExpressionSyntax value && !ValueConversions.Preserves(value, model, IsReturnConversion))
            {
                return false;
            }
            if (!SupportedAncestor(node, expression, model))
            {
                return false;
            }
            if (node is ReturnStatementSyntax || node == body)
            {
                return true;
            }
        }

        return false;
    }

    static bool IsEnvelope(ITypeSymbol type) => type.Is(EventWrapperType.MetadataName) || type.Is(ReturnedEventList.ConcurrencyEvents);

    static bool IsSequenceEffect(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => IsEffectArray(array),
        INamedTypeSymbol named when named.Is("System.Collections.Generic.List`1") || named.Is("System.Collections.Generic.IEnumerable`1") => IsEffect(named.TypeArguments[0]),
        _ => false
    };

    static bool IsEffectArray(IArrayTypeSymbol array) =>
        array.ElementType.SpecialType == SpecialType.System_Object || IsEffect(array.ElementType);

    static bool SupportedAncestor(SyntaxNode node, ExpressionSyntax returned, SemanticModel model) => node switch
    {
        ReturnStatementSyntax or ArrowExpressionClauseSyntax or ParenthesizedExpressionSyntax or ArgumentSyntax or ArgumentListSyntax or ExpressionElementSyntax => true,
        BaseObjectCreationExpressionSyntax creation => creation == returned || ReturnContainer(creation, model),
        TupleExpressionSyntax tuple => SupportedTuple(tuple, model),
        CollectionExpressionSyntax collection => collection.Elements.All(element => element is ExpressionElementSyntax item && DirectEffect(item.Expression, model)),
        InitializerExpressionSyntax initializer => initializer.Expressions.All(item => DirectEffect(item, model)),
        ArrayCreationExpressionSyntax { Initializer: not null } or ImplicitArrayCreationExpressionSyntax => true,
        InvocationExpressionSyntax invocation => IsReturnEnvelope(invocation, model),
        AwaitExpressionSyntax awaited => IsAwaitedTask(awaited, model),
        _ => false
    };

    static bool IsAwaitedTask(AwaitExpressionSyntax awaited, SemanticModel model) =>
        model.GetTypeInfo(awaited.Expression).Type is { } type &&
            (type.Is("System.Threading.Tasks.Task`1") || type.Is("System.Threading.Tasks.ValueTask`1"));

    static bool IsReturnEnvelope(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        (IsOneOfEventEnvelope(method) || (method.Name == "FromResult" &&
            (method.ContainingType.Is("System.Threading.Tasks.Task") || method.ContainingType.Is("System.Threading.Tasks.ValueTask"))));

    static bool IsReturnConversion(IMethodSymbol method) =>
        method.ContainingType.Is("Cratis.Monads.Result`2") || IsOneOfEventEnvelope(method);

    static bool IsOneOfEventEnvelope(IMethodSymbol method) =>
        method.ContainingType.Is("OneOf.OneOf`2") &&
        (method.Name == "FromT0" || method.Name == "FromT1" || method.Name == "op_Implicit") &&
        method.Parameters is [{ Type: var type }] && type.Is(EventWrapperType.MetadataName);

    static bool ReturnContainer(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        model.GetTypeInfo(creation).Type is { } type &&
        ((type.Is("System.Collections.Generic.List`1") && creation.ArgumentList?.Arguments.Count is not > 0 && creation.Initializer is not null) ||
         ((type.Is("System.Threading.Tasks.ValueTask`1") || type.Is("Cratis.Monads.Result`2")) && creation.Initializer is null));

    static bool SupportedTuple(TupleExpressionSyntax tuple, SemanticModel model)
    {
        if (TupleType(tuple, model) is not { } type || type.TupleElements.Length != tuple.Arguments.Count ||
            !type.TupleElements.Zip(tuple.Arguments).Where(pair => IsEffect(pair.First.Type))
                .All(pair => DirectEffect(pair.Second.Expression, model, allowCollection: true)))
        {
            return false;
        }

        var effects = type.TupleElements.Count(element => IsEffect(element.Type));
        if (type.TupleElements.Any(element => IsIdentity(element.Type)))
        {
            return SupportedIdentityTuple(type, effects);
        }
        return effects > 0 && type.TupleElements.Length - effects <= 1 && SupportedResponses(tuple, type, model);
    }

    static bool SupportedIdentityTuple(INamedTypeSymbol type, int effects) =>
        type.TupleElements.Length == 2 && effects == 1 && type.TupleElements.Count(element => IsIdentity(element.Type)) == 1;

    static bool SupportedResponses(TupleExpressionSyntax tuple, INamedTypeSymbol type, SemanticModel model) =>
        type.TupleElements.Zip(tuple.Arguments).Where(pair => !IsEffect(pair.First.Type))
            .All(pair => TupleResponseIdentity.CannotOverrideIdentity(pair.First.Type) &&
                ValueConversions.Preserves(pair.Second.Expression, model, _ => false));

    static INamedTypeSymbol? TupleType(TupleExpressionSyntax tuple, SemanticModel model)
    {
        var type = model.GetTypeInfo(tuple);

        if (type.ConvertedType is INamedTypeSymbol { IsTupleType: true } target)
        {
            return target;
        }
        for (var conversion = model.GetOperation(tuple)?.Parent as IConversionOperation; conversion is not null; conversion = conversion.Parent as IConversionOperation)
        {
            if (ConvertedTuple(conversion) is { } converted)
            {
                return converted;
            }
        }

        return type.ConvertedType is null && type.Type is INamedTypeSymbol { IsTupleType: true } inferred ? inferred : null;
    }

    static INamedTypeSymbol? ConvertedTuple(IConversionOperation conversion) => conversion switch
    {
        { Type: INamedTypeSymbol { IsTupleType: true } converted } => converted,
        { OperatorMethod: { Parameters: [{ Type: INamedTypeSymbol { IsTupleType: true } payload }] } method } when method.ContainingType.Is("Cratis.Monads.Result`2") => payload,
        _ => null
    };

    static bool DirectEffect(ExpressionSyntax expression, SemanticModel model, bool allowCollection = false)
    {
        if (!ValueConversions.Preserves(expression, model, method => method.ContainingType.Is("Cratis.Monads.Result`2")))
        {
            return false;
        }
        while (expression is ParenthesizedExpressionSyntax parentheses)
        {
            expression = parentheses.Expression;
        }
        if (allowCollection && CollectionElements(expression, model) is { } elements)
        {
            return elements.All(item => DirectEffect(item, model));
        }
        if (expression is not BaseObjectCreationExpressionSyntax creation || model.GetTypeInfo(creation).Type is not { } type)
        {
            return false;
        }

        return IsConstructedEffect(creation, type, model);
    }

    static IEnumerable<ExpressionSyntax>? CollectionElements(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        CollectionExpressionSyntax collection when collection.Elements.All(element => element is ExpressionElementSyntax) =>
            collection.Elements.OfType<ExpressionElementSyntax>().Select(element => element.Expression),
        ArrayCreationExpressionSyntax { Initializer: { } array } => array.Expressions,
        ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions,
        BaseObjectCreationExpressionSyntax { Initializer: { } list } listCreation when IsPlainList(listCreation, model) => list.Expressions,
        _ => null
    };

    static bool IsPlainList(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        model.GetTypeInfo(creation).Type.Is("System.Collections.Generic.List`1") && creation.ArgumentList?.Arguments.Count is not > 0;

    static bool IsConstructedEffect(BaseObjectCreationExpressionSyntax creation, ITypeSymbol type, SemanticModel model)
    {
        if (type.Is(ReturnedEventList.ConcurrencyEvents))
        {
            return creation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Body is { } body && ReturnedEventList.Read(body, model) is not null;
        }

        return EventReader.IsEvent(type) ? InlineProductionShape.IsSupported(creation, model) : ExplicitEventDestination.Read(creation, model) is not null;
    }
}
