// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads the command response a scenario asserts with concrete values.
/// </summary>
/// <param name="sources">The reader proving a value is an exact literal of the type it is compared with.</param>
/// <param name="models">The <see cref="SemanticModels"/> every declaration of the scenario is read through.</param>
/// <remarks>
/// A response is stated only when every read of <c>CommandResult&lt;T&gt;.Response</c> in the assertions is an
/// unconditional equality with a concrete value: the whole response, or one field of a record response. Anything else -
/// a null check, a comparison with a value the scenario computes or reads from the run, or an assertion held to a
/// condition or after an early exit - says something the document cannot state, so nothing is stated rather than
/// part of it. The result whose response is read must be the one the scenario's own action produced: the variable
/// the awaited <c>Execute</c> is assigned to, written nowhere else. Writes through its member or element chains
/// (assignments, increments, decrements, <see langword="ref"/> and <see langword="out"/>) and aliases of it or reference values read through it
/// also withhold the response. Null-forgiving operators and <see langword="in"/> arguments are reads. Mutation through ordinary
/// method calls is outside this syntax-and-symbol proof.
/// <para>
/// Values a command generates inside <c>Handle()</c> cannot be compared with a literal at all: Arc offers no way
/// for a scenario to choose them, so a scenario can only relate them to other values of the same run.
/// </para>
/// </remarks>
public class SpecificationReturnValues(MappingSourceReader sources, SemanticModels models)
{
    const string CommandResultOfT = "Cratis.Arc.Commands.CommandResult`1";
    const string ResponseProperty = "Response";
    const string ShouldEqualityExtensions = "Cratis.Specifications.ShouldEqualityExtensions";
    const string ShouldEqual = "ShouldEqual";
    const string XunitAssert = "Xunit.Assert";
    const string AssertEqual = "Equal";
    const string Expected = "expected";
    const string Actual = "actual";

    static readonly HashSet<SyntaxKind> _increments =
    [
        SyntaxKind.PreIncrementExpression,
        SyntaxKind.PreDecrementExpression,
        SyntaxKind.PostIncrementExpression,
        SyntaxKind.PostDecrementExpression
    ];

    static readonly HashSet<SyntaxKind> _writingArguments = [SyntaxKind.RefKeyword, SyntaxKind.OutKeyword];

    /// <summary>
    /// Determines whether an expression reads the response of a command result.
    /// </summary>
    /// <param name="member">The member access to check.</param>
    /// <param name="semanticModel">The semantic model of the tree the access lives in.</param>
    /// <returns>True when the member is <c>CommandResult.Response</c>.</returns>
    public static bool ReadsResponse(MemberAccessExpressionSyntax member, SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(member).Symbol is IPropertySymbol property &&
        string.Equals(property.Name, ResponseProperty, StringComparison.Ordinal) &&
        (property.ContainingType.Is("Cratis.Arc.Commands.CommandResult") || property.ContainingType.FindBase("Cratis.Arc.Commands.CommandResult") is not null);

    /// <summary>
    /// Reads the response the assertions state.
    /// </summary>
    /// <param name="bodies">The assertion bodies with the semantic model each is read through.</param>
    /// <param name="scenario">The type declaring the scenario.</param>
    /// <param name="action">The invocation issuing the scenario's command.</param>
    /// <returns>The response, or <see langword="null"/> when a response read is not a recoverable equality.</returns>
    public SpecificationReturnModel? Read(IEnumerable<(SyntaxNode Body, SemanticModel Model)> bodies, INamedTypeSymbol scenario, InvocationExpressionSyntax action)
    {
        if (models.For(action.SyntaxTree) is not { } actionModel || ResultOf(action, actionModel) is not { } result ||
            !WrittenOnlyBy(result, action, scenario))
        {
            return null;
        }

        string? responseType = null;
        LiteralSource? scalar = null;
        var fields = new List<PropertyMappingModel>();
        var any = false;

        foreach (var (body, semanticModel) in bodies)
        {
            foreach (var access in body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Where(member => ReadsResponse(member, semanticModel)))
            {
                any = true;
                if (!SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(Receiver(access.Expression)).Symbol?.OriginalDefinition, result.OriginalDefinition) ||
                    semanticModel.GetSymbolInfo(access).Symbol is not IPropertySymbol { ContainingType: { } commandResult } ||
                    !commandResult.Is(CommandResultOfT) || commandResult.TypeArguments is not [var type] ||
                    type.NullableAnnotation == NullableAnnotation.Annotated ||
                    (responseType ??= ResponseTypes.NameOf(type)) != ResponseTypes.NameOf(type))
                {
                    return null;
                }

                type = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                var subject = (ExpressionSyntax)access;
                IPropertySymbol? field = null;
                if (Outer(subject) is MemberAccessExpressionSyntax fieldAccess && fieldAccess.Expression == Wrapper(subject) &&
                    semanticModel.GetSymbolInfo(fieldAccess).Symbol is IPropertySymbol property &&
                    type is INamedTypeSymbol { IsRecord: true } record && record.FindBase(WellKnownTypeNames.ConceptAs) is null &&
                    SymbolEqualityComparer.Default.Equals(property.ContainingType, record) &&
                    record.DeclaredProperties().Any(declared => SymbolEqualityComparer.Default.Equals(declared, property)))
                {
                    field = property;
                    subject = fieldAccess;
                }

                if (ExpectedOf(subject, semanticModel, body) is not { } expected)
                {
                    return null;
                }

                if (field is not null)
                {
                    if (scalar is not null || !TryAdd(fields, field.Name, Literal(expected, field.Type, semanticModel)))
                    {
                        return null;
                    }

                    continue;
                }

                if (type is INamedTypeSymbol { IsRecord: true } whole && whole.FindBase(WellKnownTypeNames.ConceptAs) is null)
                {
                    if (scalar is not null || !TryReadRecord(expected, whole, semanticModel, fields))
                    {
                        return null;
                    }

                    continue;
                }

                var value = Literal(expected, type, semanticModel);
                if (value is null || fields.Count > 0 || (scalar is not null && !Equals(scalar.Value, value.Value)))
                {
                    return null;
                }

                scalar = value;
            }
        }

        return any && responseType is not null && (scalar is not null || fields.Count > 0)
            ? new(responseType, scalar, fields)
            : null;
    }

    static SyntaxNode? Outer(ExpressionSyntax expression) => Wrapper(expression).Parent;

    static ExpressionSyntax Receiver(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Receiver(parenthesized.Expression),
        CastExpressionSyntax cast => Receiver(cast.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppress => Receiver(suppress.Operand),
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } conversion => Receiver(conversion.Left),
        _ => expression
    };

    static ISymbol? ResultOf(InvocationExpressionSyntax action, SemanticModel semanticModel)
    {
        SyntaxNode node = action;
        while (node.Parent is AwaitExpressionSyntax or ParenthesizedExpressionSyntax)
        {
            node = node.Parent;
        }

        var symbol = node.Parent switch
        {
            AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment when assignment.Right == node =>
                semanticModel.GetSymbolInfo(assignment.Left).Symbol,
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } => semanticModel.GetDeclaredSymbol(declarator),
            _ => null
        };

        return symbol is IFieldSymbol or IPropertySymbol or ILocalSymbol ? symbol : null;
    }

    static ExpressionSyntax Wrapper(ExpressionSyntax expression)
    {
        while (expression.Parent is ParenthesizedExpressionSyntax or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            expression = (ExpressionSyntax)expression.Parent;
        }

        return expression;
    }

    static ExpressionSyntax? ExpectedOf(ExpressionSyntax subject, SemanticModel semanticModel, SyntaxNode body)
    {
        var wrapped = Wrapper(subject);
        if (wrapped.Parent is MemberAccessExpressionSyntax receiver && receiver.Expression == wrapped &&
            receiver.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] } should && should.Expression == receiver &&
            argument.NameColon is null && argument.RefKindKeyword.IsKind(SyntaxKind.None) &&
            semanticModel.GetSymbolInfo(should).Symbol is IMethodSymbol { ReducedFrom: { } extension } &&
            string.Equals(extension.Name, ShouldEqual, StringComparison.Ordinal) &&
            extension.ContainingType.Is(ShouldEqualityExtensions) && extension.Parameters.Length == 2 &&
            StepsTaken.AlwaysCompletesThrough(should, body))
        {
            return argument.Expression;
        }

        if (wrapped.Parent is ArgumentSyntax actual && actual.Parent is ArgumentListSyntax { Arguments.Count: 2 } list &&
            list.Parent is InvocationExpressionSyntax equal &&
            semanticModel.GetSymbolInfo(equal).Symbol is IMethodSymbol { Parameters: [var first, var second] } method &&
            string.Equals(method.Name, AssertEqual, StringComparison.Ordinal) && method.ContainingType.Is(XunitAssert) &&
            string.Equals(first.Name, Expected, StringComparison.Ordinal) && string.Equals(second.Name, Actual, StringComparison.Ordinal) &&
            SymbolEqualityComparer.Default.Equals(first.Type, second.Type) &&
            (method.OriginalDefinition.Parameters[0].Type is ITypeParameterSymbol || first.Type.SpecialType == SpecialType.System_String) &&
            StepsTaken.AlwaysCompletesThrough(equal, body))
        {
            var actualIndex = list.Arguments.IndexOf(actual);
            var expectedArgument = list.Arguments[1 - actualIndex];
            var actualName = actual.NameColon?.Name.Identifier.ValueText ?? (actualIndex == 1 ? Actual : Expected);
            var expectedName = expectedArgument.NameColon?.Name.Identifier.ValueText ?? (actualIndex == 1 ? Expected : Actual);
            return actualName == Actual && expectedName == Expected ? expectedArgument.Expression : null;
        }

        return null;
    }

    static bool TryAdd(List<PropertyMappingModel> fields, string name, LiteralSource? value)
    {
        if (value is null)
        {
            return false;
        }

        if (fields.Find(field => string.Equals(field.Property, name, StringComparison.Ordinal)) is { } existing)
        {
            return Equals(((LiteralSource)existing.Source).Value, value.Value);
        }

        fields.Add(new(name, value));
        return true;
    }

    static bool IsResult(ExpressionSyntax expression, ISymbol result, SemanticModel semanticModel) =>
        SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(Receiver(expression)).Symbol?.OriginalDefinition, result.OriginalDefinition);

    static bool RootedAtResult(ExpressionSyntax expression, ISymbol result, SemanticModel semanticModel) =>
        IsResult(expression, result, semanticModel) ||
        (ReceiverOf(Receiver(expression)) is { } receiver && RootedAtResult(receiver, result, semanticModel));

    static ExpressionSyntax? ReceiverOf(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax member => member.Expression,
        ElementAccessExpressionSyntax element => element.Expression,
        ConditionalAccessExpressionSyntax conditional => conditional.Expression,
        MemberBindingExpressionSyntax or ElementBindingExpressionSyntax => BoundBy(expression)?.Expression,
        _ => null
    };

    static ConditionalAccessExpressionSyntax? BoundBy(ExpressionSyntax binding) =>
        binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault(access => access.WhenNotNull.Span.Contains(binding.Span));

    static bool WritesResult(SyntaxNode node, ISymbol result, InvocationExpressionSyntax action, SemanticModel semanticModel) =>
        WriteTarget(node) is { } target && !AssignsAction(node, result, action, semanticModel) && WritesTarget(target, result, semanticModel);

    static ExpressionSyntax? WriteTarget(SyntaxNode node) => node switch
    {
        AssignmentExpressionSyntax assignment => assignment.Left,
        ArgumentSyntax argument when _writingArguments.Contains(argument.RefKindKeyword.Kind()) => argument.Expression,
        RefExpressionSyntax reference => reference.Expression,
        PrefixUnaryExpressionSyntax prefix when _increments.Contains(prefix.Kind()) => prefix.Operand,
        PostfixUnaryExpressionSyntax postfix when _increments.Contains(postfix.Kind()) => postfix.Operand,
        _ => null
    };

    static bool AssignsAction(SyntaxNode node, ISymbol result, InvocationExpressionSyntax action, SemanticModel semanticModel) =>
        node is AssignmentExpressionSyntax assignment && IsResult(assignment.Left, result, semanticModel) &&
        assignment.Right.DescendantNodesAndSelf().Contains(action);

    static bool WritesTarget(ExpressionSyntax target, ISymbol result, SemanticModel semanticModel) =>
        Receiver(target) is TupleExpressionSyntax tuple
            ? tuple.Arguments.Any(argument => WritesTarget(argument.Expression, result, semanticModel))
            : RootedAtResult(target, result, semanticModel);

    static bool AliasesResult(SyntaxNode node, ISymbol result, SemanticModel semanticModel) =>
        AliasesPattern(node, result, semanticModel) || AliasesElements(node, result, semanticModel) || CopiesResult(node, result, semanticModel);

    static bool AliasesElements(SyntaxNode node, ISymbol result, SemanticModel semanticModel) =>
        node is CommonForEachStatementSyntax each && RootedAtResult(each.Expression, result, semanticModel) &&
        IsReference(semanticModel.GetForEachStatementInfo(each).ElementType);

    static bool CopiesResult(SyntaxNode node, ISymbol result, SemanticModel semanticModel) =>
        Copied(node) is { } copied && Flowing(copied).Any(value => RootedAtResult(value, result, semanticModel) &&
            (IsResult(value, result, semanticModel) || IsReference(semanticModel.GetTypeInfo(value).Type)));

    static IEnumerable<ExpressionSyntax> Flowing(ExpressionSyntax expression)
    {
        var value = Receiver(expression);
        return Branches(value) is { } branches ? [value, .. branches.SelectMany(Flowing)] : [value];
    }

    static IEnumerable<ExpressionSyntax>? Branches(ExpressionSyntax value) => value switch
    {
        ConditionalExpressionSyntax conditional => [conditional.WhenTrue, conditional.WhenFalse],
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } coalesce => [coalesce.Left, coalesce.Right],
        _ => Elements(value)
    };

    static IEnumerable<ExpressionSyntax>? Elements(ExpressionSyntax value) => value switch
    {
        TupleExpressionSyntax tuple => tuple.Arguments.Select(argument => argument.Expression),
        CollectionExpressionSyntax collection => CollectionElements(collection),
        SwitchExpressionSyntax switched => switched.Arms.Select(arm => arm.Expression),
        _ => null
    };

    static IEnumerable<ExpressionSyntax> CollectionElements(CollectionExpressionSyntax collection) =>
        collection.Elements.OfType<ExpressionElementSyntax>().Select(element => element.Expression);

    static ExpressionSyntax? Copied(SyntaxNode node) => node switch
    {
        AssignmentExpressionSyntax assignment => assignment.Right,
        EqualsValueClauseSyntax initializer => initializer.Value,
        ReturnStatementSyntax returned => returned.Expression,
        YieldStatementSyntax yielded => yielded.Expression,
        _ => Produced(node)
    };

    static ExpressionSyntax? Produced(SyntaxNode node) => node switch
    {
        ArrowExpressionClauseSyntax arrow => arrow.Expression,
        LambdaExpressionSyntax lambda => lambda.ExpressionBody,
        _ => null
    };

    static bool AliasesPattern(SyntaxNode node, ISymbol result, SemanticModel semanticModel) =>
        PatternsOf(node) is ({ } subject, var patterns) && RootedAtResult(subject, result, semanticModel) && patterns.Any(DeclaresVariable);

    static (ExpressionSyntax? Subject, IEnumerable<PatternSyntax> Patterns) PatternsOf(SyntaxNode node) => node switch
    {
        IsPatternExpressionSyntax pattern => (pattern.Expression, [pattern.Pattern]),
        SwitchExpressionSyntax expression => (expression.GoverningExpression, expression.Arms.Select(arm => arm.Pattern)),
        SwitchStatementSyntax statement => (statement.Expression, CasePatterns(statement)),
        _ => (null, [])
    };

    static IEnumerable<PatternSyntax> CasePatterns(SwitchStatementSyntax statement) =>
        statement.Sections.SelectMany(section => section.Labels).OfType<CasePatternSwitchLabelSyntax>().Select(label => label.Pattern);

    static bool DeclaresVariable(PatternSyntax pattern) => pattern.DescendantNodes().OfType<SingleVariableDesignationSyntax>().Any();

    static bool IsReference(ITypeSymbol? type) => type is { IsReferenceType: true } && type.SpecialType != SpecialType.System_String;

    bool WrittenOnlyBy(ISymbol result, InvocationExpressionSyntax action, INamedTypeSymbol scenario)
    {
        var declarations = new List<SyntaxReference>();
        for (var current = scenario; current is not null; current = current.BaseType)
        {
            declarations.AddRange(current.DeclaringSyntaxReferences);
        }

        declarations.AddRange(result.ContainingType?.DeclaringSyntaxReferences ?? []);
        var nodes = declarations.Select(reference => reference.GetSyntax()).Distinct().SelectMany(declaration => declaration.DescendantNodes());
        return !nodes.Any(node => models.For(node.SyntaxTree) is { } semanticModel &&
            (WritesResult(node, result, action, semanticModel) || AliasesResult(node, result, semanticModel)));
    }

    bool TryReadRecord(ExpressionSyntax expected, INamedTypeSymbol record, SemanticModel semanticModel, List<PropertyMappingModel> fields)
    {
        if (MappingSourceReader.Unwrap(expected) is not BaseObjectCreationExpressionSyntax { Initializer: null } creation ||
            !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(creation).Type?.WithNullableAnnotation(NullableAnnotation.None), record.WithNullableAnnotation(NullableAnnotation.None)))
        {
            return false;
        }

        var declared = record.DeclaredProperties().ToList();
        var stated = SpecificationValues.Stated(creation, semanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol).ToList();
        if (stated.Count != declared.Count)
        {
            return false;
        }

        foreach (var (name, expression) in stated)
        {
            if (declared.Find(property => string.Equals(property.Name, name, StringComparison.Ordinal)) is not { } property ||
                !TryAdd(fields, property.Name, Literal(expression, property.Type, semanticModel)))
            {
                return false;
            }
        }

        return fields.Count >= declared.Count;
    }

    LiteralSource? Literal(ExpressionSyntax expression, ITypeSymbol type, SemanticModel semanticModel)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return null;
        }

        var expected = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        if (sources.ReadQueryLiteral(expression, semanticModel, expected, expected) is { } literal)
        {
            return literal;
        }

        var unwrapped = expression;
        while (unwrapped is ParenthesizedExpressionSyntax parenthesized)
        {
            unwrapped = parenthesized.Expression;
        }

        return expected.SpecialType is SpecialType.System_String or SpecialType.System_Int32 or SpecialType.System_Decimal or SpecialType.System_Boolean &&
            SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(unwrapped).Type, expected) &&
            semanticModel.GetConstantValue(unwrapped) is { HasValue: true, Value: { } constant }
            ? new LiteralSource(constant)
            : null;
    }
}
