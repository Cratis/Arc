// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Proves the non-escaping list and BeforeFirst scope shape emitted by Stage.</summary>
/// <param name="body">The complete handler body.</param>
/// <param name="model">The semantic model binding all references.</param>
internal class ReturnedEventList(BlockSyntax body, SemanticModel model)
{
    /// <summary>The supported Chronicle return envelope.</summary>
    internal const string ConcurrencyEvents = "Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes";

    readonly List<ExplicitEventDestination> _events = [];
    readonly HashSet<SyntaxNode> _uses = [];
    readonly HashSet<ISymbol> _guards = new(SymbolEqualityComparer.Default);
    ILocalSymbol? _list;
    IParameterSymbol? _identity;
    ProducesScope? _scope;
    bool _unconditional;

    /// <summary>Gets the proven, directly constructed event envelopes.</summary>
    internal IReadOnlyList<ExplicitEventDestination> Events => _events;

    /// <summary>Reads one direct list return, rejecting unaccounted-for list uses and scopes.</summary>
    /// <param name="body">The complete handler body.</param>
    /// <param name="model">The semantic model binding all references.</param>
    /// <returns>The proof, or null.</returns>
    internal static ReturnedEventList? Read(SyntaxNode body, SemanticModel model)
    {
        if (body is not BlockSyntax block)
        {
            return null;
        }
        var proof = new ReturnedEventList(block, model);
        return proof.ReadBody() ? proof : null;
    }

    /// <summary>Checks whether the proven returned list carries this event or wrapper.</summary>
    /// <param name="expression">The constructed event or wrapper.</param>
    /// <param name="body">The handler body.</param>
    /// <param name="model">The semantic model.</param>
    /// <returns>True only for an item added by the list proof.</returns>
    internal static bool Carries(ExpressionSyntax expression, SyntaxNode body, SemanticModel model) =>
        Read(body, model)?._events.Exists(item => item.Wrapper == expression || item.Event == expression) == true;

    static bool IsDirectReturn(ExpressionSyntax returned, BaseObjectCreationExpressionSyntax? envelope) => envelope is null || returned == envelope ||
        (returned is TupleExpressionSyntax { Arguments.Count: 2 } tuple && tuple.Arguments.Any(argument => argument.Expression == envelope));

    static ExpressionSyntax? ScopeDestination(ConditionalExpressionSyntax conditional) => conditional.WhenTrue is ArrayCreationExpressionSyntax { Initializer.Expressions: [var destination] } &&
        conditional.WhenFalse is CollectionExpressionSyntax { Elements.Count: 0 } ? destination : null;

    bool ReadBody()
    {
        var returned = HandlerReturn();
        if (returned is null || model.GetEnclosingSymbol(body.SpanStart)?.ContainingType is not { } command)
        {
            return false;
        }
        _scope = new(model, command, null, null);
        var envelopes = ReturnEnvelopes(returned);
        if (envelopes.Length > 1)
        {
            return false;
        }
        var envelope = envelopes.SingleOrDefault();
        if (!IsDirectReturn(returned, envelope) || !ReadList(envelope?.ArgumentList?.Arguments.FirstOrDefault()?.Expression ?? returned))
        {
            return false;
        }
        return ReadContents() && HasValidScopes(envelope);
    }

    BaseObjectCreationExpressionSyntax[] ReturnEnvelopes(ExpressionSyntax returned) => returned.DescendantNodesAndSelf()
        .OfType<BaseObjectCreationExpressionSyntax>().Where(creation => model.GetTypeInfo(creation).Type.Is(ConcurrencyEvents)).ToArray();

    bool HasValidScopes(BaseObjectCreationExpressionSyntax? envelope) => envelope is null || (!_unconditional && HasScopes(envelope));

    bool ReadContents() => body.Statements.SkipLast(1).All(ReadStatement) && ReadIdentity() && HasOnlyKnownUses();

    ExpressionSyntax? HandlerReturn() => body.DescendantNodes().OfType<ReturnStatementSyntax>().Count() == 1
        ? (body.Statements.LastOrDefault() as ReturnStatementSyntax)?.Expression : null;

    bool ReadList(ExpressionSyntax expression)
    {
        if (expression is not IdentifierNameSyntax)
        {
            return false;
        }
        if (model.GetSymbolInfo(expression).Symbol is not ILocalSymbol local || !CommandAuthoringReader.IsUnchanged(local, body, model))
        {
            return false;
        }
        if (LocalValueOrigins.Values(local, model).ToArray() is not [BaseObjectCreationExpressionSyntax initializer] || !IsEmptyList(initializer))
        {
            return false;
        }
        _list = local;
        _uses.Add(expression);
        return true;
    }

    bool IsEmptyList(BaseObjectCreationExpressionSyntax initializer) =>
        model.GetTypeInfo(initializer).Type is INamedTypeSymbol type && type.Is("System.Collections.Generic.List`1") &&
        type.TypeArguments[0].Is(EventWrapperType.MetadataName) && initializer.ArgumentList?.Arguments.Count == 0 && initializer.Initializer is null;

    bool ReadStatement(StatementSyntax statement)
    {
        if (statement is LocalDeclarationStatementSyntax declaration)
        {
            return declaration.Declaration.Variables.All(IsKnownLocal);
        }
        if (statement is IfStatementSyntax branch)
        {
            return ReadBranch(branch);
        }
        _unconditional = true;
        return ReadAddition(statement);
    }

    bool IsKnownLocal(VariableDeclaratorSyntax declaration)
    {
        if (SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(declaration), _list))
        {
            return true;
        }
        return declaration.Initializer?.Value is { } value && new ProducesConditionResolver(new()).ReadGuard(value, _scope!, string.Empty) is not null;
    }

    bool ReadBranch(IfStatementSyntax branch)
    {
        if (branch.Else is not null || branch.Condition is not IdentifierNameSyntax)
        {
            return false;
        }
        if (model.GetSymbolInfo(branch.Condition).Symbol is not ILocalSymbol guard)
        {
            return false;
        }
        if (new ProducesConditionResolver(new()).ReadGuard(branch.Condition, _scope!, string.Empty) is null)
        {
            return false;
        }
        _guards.Add(guard);
        return ReadAddition(branch.Statement is BlockSyntax { Statements: [var only] } ? only : branch.Statement);
    }

    bool ReadAddition(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member, ArgumentList.Arguments: [var argument] } invocation })
        {
            return false;
        }
        if (!IsListAdd(invocation, member.Expression) || argument.Expression is not BaseObjectCreationExpressionSyntax wrapper)
        {
            return false;
        }
        if (ExplicitEventDestination.Read(wrapper, model) is not { } item)
        {
            return false;
        }
        _events.Add(item);
        _uses.Add(member.Expression);
        return true;
    }

    bool IsListAdd(InvocationExpressionSyntax invocation, ExpressionSyntax receiver) =>
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, _list) &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && method.Name == "Add" && method.ContainingType.Is("System.Collections.Generic.List`1");

    bool ReadIdentity()
    {
        if (_events.Count == 0)
        {
            return false;
        }
        if (model.GetSymbolInfo(_events[0].Destination).Symbol is not IParameterSymbol identity || !IsGeneratedIdentity(identity))
        {
            return false;
        }
        _identity = identity;
        return _events.TrueForAll(SharesDestination);
    }

    bool IsGeneratedIdentity(IParameterSymbol identity) => identity.HasAttribute(WellKnownTypeNames.GeneratedValueAttribute) &&
        ReturnedProductionShape.IsIdentity(identity.Type) && CommandAuthoringReader.IsUnchanged(identity, body, model);

    bool SharesDestination(ExplicitEventDestination item) => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(item.Destination).Symbol, _identity);

    bool HasOnlyKnownUses() => body.DescendantNodes().OfType<IdentifierNameSyntax>().All(identifier =>
        !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, _list) || _uses.Contains(identifier));

    bool HasScopes(BaseObjectCreationExpressionSyntax envelope)
    {
        if (envelope.ArgumentList?.Arguments is not [_, var scopes] || scopes.Expression is not InvocationExpressionSyntax select)
        {
            return false;
        }
        if (!IsEnumerable(select, "Select", 2))
        {
            return false;
        }
        if (select.ArgumentList.Arguments[0].Expression is not InvocationExpressionSyntax distinct || !IsEnumerable(distinct, "Distinct", 1))
        {
            return false;
        }
        if (select.ArgumentList.Arguments[1].Expression is not SimpleLambdaExpressionSyntax { Body: BaseObjectCreationExpressionSyntax pair } lambda)
        {
            return false;
        }
        return ScopePair(pair, model.GetDeclaredSymbol(lambda.Parameter)) && ScopeSources(distinct.ArgumentList.Arguments[0].Expression);
    }

    bool ScopePair(BaseObjectCreationExpressionSyntax pair, ISymbol? parameter)
    {
        if (model.GetTypeInfo(pair).Type is not INamedTypeSymbol type)
        {
            return false;
        }
        if (!type.Is("System.Collections.Generic.KeyValuePair`2") || !type.TypeArguments[0].Is("Cratis.Chronicle.Events.EventSourceId"))
        {
            return false;
        }
        return pair.ArgumentList?.Arguments is [var key, { Expression: BaseObjectCreationExpressionSyntax scope }] &&
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(key.Expression).Symbol, parameter) && ScopeBeforeFirst(scope, parameter);
    }

    bool ScopeBeforeFirst(BaseObjectCreationExpressionSyntax scope, ISymbol? parameter)
    {
        if (!model.GetTypeInfo(scope).Type.Is("Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope") ||
            scope.ArgumentList?.Arguments is not [{ Expression: MemberAccessExpressionSyntax beforeFirst }, var source])
        {
            return false;
        }
        return IsBeforeFirst(beforeFirst) && IsScopeDestination(scope, source) && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(source.Expression).Symbol, parameter);
    }

    bool IsBeforeFirst(MemberAccessExpressionSyntax expression) => model.GetSymbolInfo(expression).Symbol is IFieldSymbol field &&
        field.Name == "BeforeFirst" && field.ContainingType.Is("Cratis.Chronicle.Events.EventSequenceNumber");

    bool IsScopeDestination(BaseObjectCreationExpressionSyntax scope, ArgumentSyntax source) => model.GetOperation(scope) is IObjectCreationOperation operation &&
        operation.Arguments.Any(argument => argument.Parameter?.Name == "EventSourceId" && argument.Syntax == source);

    bool IsEnumerable(InvocationExpressionSyntax call, string name, int count) =>
        call.ArgumentList.Arguments.Count == count && model.GetSymbolInfo(call).Symbol is IMethodSymbol method && method.Name == name && method.ContainingType.Is("System.Linq.Enumerable");

    bool ScopeSources(ExpressionSyntax expression)
    {
        if (expression is not CollectionExpressionSyntax { Elements: [SpreadElementSyntax spread] } ||
            MappingSourceReader.Unwrap(spread.Expression) is not ConditionalExpressionSyntax conditional)
        {
            return false;
        }
        var destination = ScopeDestination(conditional);
        if (destination is null)
        {
            return false;
        }
        return SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(destination).Symbol, _identity) && HasScopeGuards(conditional.Condition);
    }

    bool HasScopeGuards(ExpressionSyntax condition)
    {
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        return ScopeGuard(condition, seen) && _guards.SetEquals(seen);
    }

    bool ScopeGuard(ExpressionSyntax expression, HashSet<ISymbol> seen) => MappingSourceReader.Unwrap(expression) switch
    {
        IdentifierNameSyntax name when model.GetSymbolInfo(name).Symbol is ILocalSymbol local => seen.Add(local) || seen.Contains(local),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression) => ScopeGuard(binary.Left, seen) && ScopeGuard(binary.Right, seen),
        _ => false
    };
}
