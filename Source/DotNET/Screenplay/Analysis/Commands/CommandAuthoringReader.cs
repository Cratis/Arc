// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Reads optional authoring intent without inferring behavior from arbitrary code.</summary>
/// <param name="models">The source semantic models.</param>
/// <param name="types">The application's type registry.</param>
/// <param name="paths">The portable source paths.</param>
/// <param name="diagnostics">Where unreadable shapes are reported.</param>
/// <param name="enabled">Whether authoring-only constructs are enabled.</param>
public class CommandAuthoringReader(SemanticModels models, TypeRegistry types, SourcePaths paths, ScreenplayDiagnostics diagnostics, bool enabled)
{
    readonly ScreenplayNaming _naming = new();

    /// <summary>Gets the proven sources of the command most recently read.</summary>
    public AuthoringSources Sources { get; private set; } = new();

    /// <summary>
    /// Gets whether executable event routes are admitted.
    /// </summary>
    public bool ExecutableRoutes { get; init; }

    /// <summary>
    /// Gets the types with concept validators, including unreadable rules.
    /// </summary>
    public IReadOnlySet<string> ValidatedTypes { get; init; } = new HashSet<string>();

    /// <summary>
    /// Gets the shared concept-validation admission check for explicit generation.
    /// </summary>
    internal Func<ITypeSymbol, string?> ValidationFailure { get; init; } = _ => null;

    /// <summary>
    /// Determines whether a local or parameter is never reassigned or passed by reference.
    /// </summary>
    /// <param name="symbol">The local or parameter to check.</param>
    /// <param name="body">The body, including captured assignments.</param>
    /// <param name="model">The semantic model of the body.</param>
    /// <returns>Whether every reference leaves the value unchanged.</returns>
    public static bool IsUnchanged(ISymbol symbol, SyntaxNode body, SemanticModel model) =>
        !body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(identifier => identifier.Identifier.ValueText == symbol.Name &&
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, symbol) &&
            identifier.Ancestors().TakeWhile(ancestor => ancestor is not StatementSyntax).Any(ancestor => ancestor switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left.Span.Contains(identifier.Span),
                PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression),
                PostfixUnaryExpressionSyntax postfix => postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression),
                ArgumentSyntax { RefKindKeyword.RawKind: not 0 } or RefExpressionSyntax => true,
                _ => false
            }));

    /// <summary>Reads one command's optional authoring intent.</summary>
    /// <param name="command">The command declaration.</param>
    /// <param name="handlers">The command handlers.</param>
    /// <param name="identifier">The input event source key, if proven.</param>
    /// <param name="location">The diagnostic location.</param>
    /// <returns>The optional authoring model.</returns>
    public CommandAuthoringModel? Read(INamedTypeSymbol command, IReadOnlyList<IMethodSymbol> handlers, string? identifier, string location)
    {
        location = $"{location}.{command.Name}";
        Sources = new();
        var readsReader = new CommandReadsReader(models, types, diagnostics, enabled);
        var reads = readsReader.Read(command, identifier, Sources, location);
        var result = new CommandAuthoringModel { Reads = reads.Reads, Requirements = reads.Requirements };
        result = result with { Route = new CommandRouteReader(models, types, diagnostics, enabled || ExecutableRoutes).Read(command, identifier, location) };

        var parameters = new GeneratedParameterReader(models, types, diagnostics) { Sources = Sources, ValidatedTypes = ValidatedTypes, ValidationFailure = ValidationFailure };
        parameters.ReadStrategies(handlers, location);
        result = result with { GeneratedParameters = parameters.HasParameters, UnavailableGenerated = parameters.Unavailable };

        if (!enabled)
        {
            if (ProvisionStaysInCode(command, readsReader))
            {
                Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"The provisioning behavior of command '{command.Name}' is not represented in default output; generated values and responses were left in code", location);
                return result;
            }

            if (handlers.Any(handler => ContainsOperation(handler.ReturnType)))
            {
                Report(ScreenplayDiagnosticCodes.UnreadableCommandOperation, "Returned operations are authoring-only; enable ScreenplayOptions.AuthoringOnlyConstructs to describe readable operations", location);
            }
        }

        if (handlers is not [var handler] || HandlerBodies.Of(handler).ToArray() is not [var body] || models.For(body.SyntaxTree) is not { } model)
        {
            return result;
        }

        var returned = ReturnedBy(body);
        var straight = IsStraight(body);
        parameters.Bind(command, body, model);
        var generated = parameters.Generated.ToList();
        var validatedGenerated = parameters.Validated.ToList();
        result = result with
        {
            GeneratedParameters = parameters.HasParameters,
            UnavailableGenerated = parameters.Unavailable
        };
        foreach (var variable in parameters.LegacyLocals(body))
        {
            if (variable.Initializer?.Value is not { } value || model.GetDeclaredSymbol(variable) is not ILocalSymbol local)
            {
                continue;
            }

            if (CreatesGeneratableConcept(value, local.Type, model))
            {
                if (!IsUnchanged(local, body, model))
                {
                    Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"Generated local '{local.Name}' is written after its initializer or passed by reference; its generation and dependent claims were left in code", location);
                    continue;
                }

                if (returned is null || !straight)
                {
                    continue;
                }

                if (command.DeclaredProperties().Select(property => property.Name).Concat(generated.Select(property => property.Name))
                    .Any(name => _naming.ToPropertyName(name) == _naming.ToPropertyName(local.Name)))
                {
                    Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"Generated local '{local.Name}' collides with an emitted command property; generation and dependent claims were left in code", location);
                    Sources = new();
                    return result;
                }

                // Roslyn annotates inferred reference locals as nullable even when their initializer is required.
                // Keep explicit local annotations, but resolve var from the proven generation expression instead.
                var generatedType = variable.Parent is VariableDeclarationSyntax { Type.IsVar: true }
                    ? model.GetTypeInfo(value).Type ?? local.Type
                    : local.Type;
                generated.Add(new(local.Name, types.ResolveGenerated(generatedType)));
                if (ValidatedTypes.Contains(local.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()))
                {
                    validatedGenerated.Add(local.Name);
                }
                Sources.AddGenerated(local, local.Name);
            }
        }

        if (returned is null || !straight)
        {
            if (HandlerBodies.YieldsEventSourceId(handler.ReturnType))
            {
                Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "The returned response or operations depend on control flow and were left in code", location);
            }

            return result;
        }

        result = result with { Generated = generated, GeneratedWithValidators = validatedGenerated };
        if (parameters.WrapperIdentifier(returned, body, model, generated) is { } wrapperIdentity)
        {
            return result with { Identifier = wrapperIdentity };
        }
        if (ResponseValueType(handler.ReturnType) is null)
        {
            return result;
        }

        var parts = ReadReturnedParts(command, returned, model, location);
        var responses = parts.Responses;
        result = result with { Operations = parts.Operations };
        if (responses.Count > 1)
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "Several response values are returned without a response record; no response was inferred", location);
            return result;
        }

        if (responses is not [var response])
        {
            return result;
        }

        if (Sources.UnavailableIn(response, model) is { } unavailable)
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"The response depends on unavailable generated input '{unavailable.Name}' ({unavailable.Failure}) and was left in code", location);
            return result;
        }

        var responseType = ResponseValueType(model.GetTypeInfo(response).Type ?? model.GetTypeInfo(response).ConvertedType);
        if (CreatesLegacyGeneratableConcept(response, responseType, model, parameters.HasParameters))
        {
            var name = char.ToLowerInvariant(responseType.Name[0]) + responseType.Name[1..];
            if (command.DeclaredProperties().Select(property => property.Name).Concat(generated.Select(property => property.Name))
                .Any(property => _naming.ToPropertyName(property) == _naming.ToPropertyName(name)))
            {
                Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"Generated response '{name}' collides with an emitted command property; generation and dependent claims were left in code", location);
                Sources = new();
                return result with { Generated = [], GeneratedWithValidators = [], Operations = [] };
            }

            generated.Add(new(name, types.ResolveGenerated(responseType)));
            if (ValidatedTypes.Contains(responseType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()))
            {
                validatedGenerated.Add(name);
            }
            Sources.AddExpression(response, name);
        }

        result = result with { Generated = generated, GeneratedWithValidators = validatedGenerated };
        var source = SourceOf(response, model, command);
        if (source?.Contains('.', StringComparison.Ordinal) == false && responseType is not null && SupportsResponse(responseType) &&
            IsDirectResponse(response, responseType, model))
        {
            var generatedIdentifier = generated.Exists(property => property.Name == source) && IsEventSourceIdentity(responseType) &&
                !AggregateRootBehaviors.ReachedFrom(body, model).Any();
            return result with { Response = source, Identifier = generatedIdentifier ? source : null, ResponseType = ResponseTypes.NameOf(responseType) };
        }

        if (response is BaseObjectCreationExpressionSyntax creation && model.GetTypeInfo(creation).Type is INamedTypeSymbol { IsRecord: true } record &&
            record.FindBase(WellKnownTypeNames.ConceptAs) is null && HasIdentityType(creation, record, model) &&
            (response != returned || SymbolEqualityComparer.Default.Equals(record, ResponseValueType(handler.ReturnType))) &&
            ReadRecord(creation, record, model, command) is { } fields)
        {
            return result with { ResponseFields = fields, ResponseType = ResponseTypes.NameOf(record) };
        }

        Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "The response is not a direct command property, generated concept, or fully readable response record and was left in code", location);
        return result;
    }

    /// <summary>Determines whether a type supplies an event source identity.</summary>
    /// <param name="type">The value type.</param>
    /// <returns>Whether the value has the Chronicle event source identity contract.</returns>
    public bool IsEventSourceIdentity(ITypeSymbol? type) => type is not null &&
        (type.Is("Cratis.Chronicle.Events.EventSourceId") || type.FindBase("Cratis.Chronicle.Events.EventSourceId`1") is not null);

    /// <summary>Proves that a handler has no fact-recording or external effects.</summary>
    /// <param name="handlers">The handlers of the command.</param>
    /// <param name="authoring">The recovered pure response.</param>
    /// <returns>Whether the command has no provisioning method and every body is empty or consists only of the recovered response and generated values.</returns>
    public bool HasNoFactBehavior(IReadOnlyList<IMethodSymbol> handlers, CommandAuthoringModel? authoring) => handlers.Count > 0 && handlers.All(handler =>
    {
        if (HasProvide(handler.ContainingType) ||
            HandlerBodies.Of(handler).ToArray() is not [var body])
        {
            return false;
        }

        if (body is BlockSyntax { Statements.Count: 0 })
        {
            return true;
        }

        if (ContainsOperation(handler.ReturnType) || authoring is not { Response: not null } and not { ResponseFields.Count: > 0 } ||
            MayCarryEvent(handler.ReturnType, new(SymbolEqualityComparer.Default)) ||
            models.For(body.SyntaxTree) is not { } bodyModel ||
            body.DescendantNodesAndSelf().OfType<ExpressionSyntax>().Any(expression =>
                bodyModel.GetTypeInfo(expression).Type is { } type && MayCarryEvent(type, new(SymbolEqualityComparer.Default), allowOpen: true)))
        {
            return false;
        }

        if (body is ExpressionSyntax)
        {
            return true;
        }

        return body is BlockSyntax block && block.Statements.LastOrDefault() is ReturnStatementSyntax &&
            block.Statements.All(statement => statement is ReturnStatementSyntax or LocalDeclarationStatementSyntax) &&
            block.Statements.OfType<ReturnStatementSyntax>().Count() == 1 &&
            block.Statements.OfType<LocalDeclarationStatementSyntax>().SelectMany(statement => statement.Declaration.Variables).All(variable =>
                models.For(variable.SyntaxTree) is { } model && model.GetDeclaredSymbol(variable) is ILocalSymbol local &&
                authoring.Generated.Any(property => property.Name == local.Name));
    });

    /// <summary>
    /// Determines whether a value of a type can be, or can carry, an event the pipeline appends.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="visited">The types already checked.</param>
    /// <param name="allowOpen">Whether open types such as <see cref="object"/> and interfaces are accepted, for subexpressions.</param>
    /// <returns>True for events, tuples, collections, results and unions with an event, and for types that could hold one at runtime.</returns>
    /// <remarks>
    /// A handler appends whatever event its returned value holds, whether directly, in a tuple, in a collection, or
    /// in a <c>Result</c> or <c>OneOf</c> branch. A returned <see cref="object"/>, <see langword="dynamic"/>, interface or type
    /// parameter can hold an event at runtime and is not proven free of one.
    /// </remarks>
    static bool MayCarryEvent(ITypeSymbol type, HashSet<ITypeSymbol> visited, bool allowOpen = false)
    {
        if (!visited.Add(type))
        {
            return false;
        }

        if (EventReader.IsEvent(type))
        {
            return true;
        }

        if (!allowOpen && (type.SpecialType == SpecialType.System_Object || type.TypeKind is TypeKind.Dynamic or TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Error))
        {
            return true;
        }

        return type switch
        {
            IArrayTypeSymbol array => MayCarryEvent(array.ElementType, visited, allowOpen),
            INamedTypeSymbol { IsTupleType: true } tuple => tuple.TupleElements.Any(element => MayCarryEvent(element.Type, visited, allowOpen)),
            INamedTypeSymbol named => named.TypeArguments.Any(argument => MayCarryEvent(argument, visited, allowOpen)),
            _ => false
        };
    }

    static ExpressionSyntax? ReturnedBy(SyntaxNode body) => body switch
    {
        BlockSyntax block => (block.Statements.LastOrDefault() as ReturnStatementSyntax)?.Expression,
        ExpressionSyntax expression => expression,
        _ => null
    };

    static ExpressionSyntax[] ReturnedExpressions(ExpressionSyntax returned) => returned is TupleExpressionSyntax tuple
        ? tuple.Arguments.Select(argument => argument.Expression).ToArray()
        : [returned];

    static bool IsReturnedOperation(ITypeSymbol type) => CommandOperationReader.IsOperation(type) || type.Is("Cratis.Arc.Commands.CommandOperations");

    static bool IsStraight(SyntaxNode body) => body is not BlockSyntax block ||
        (block.Statements.All(statement => statement is LocalDeclarationStatementSyntax or ReturnStatementSyntax) && block.Statements.OfType<ReturnStatementSyntax>().Count() == 1);

    static bool ProvisionStaysInCode(INamedTypeSymbol command, CommandReadsReader reads) => HasProvide(command) && !reads.ProvisionStatesReadsOnly;

    static bool HasProvide(INamedTypeSymbol command)
    {
        for (var current = command; current is not null; current = current.BaseType)
        {
            // Runtime discovery includes instance methods, but not private methods inherited from a base type.
            if (current.GetMembers("Provide").OfType<IMethodSymbol>().Any(method => !method.IsStatic &&
                (SymbolEqualityComparer.Default.Equals(current, command) || method.DeclaredAccessibility != Accessibility.Private)))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsDirectResponse(ExpressionSyntax expression, ITypeSymbol type, SemanticModel model)
    {
        if (expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax) ||
            !HasIdentityType(expression, type, model))
        {
            return false;
        }

        var sourceType = model.GetSymbolInfo(MappingSourceReader.Unwrap(expression)).Symbol switch
        {
            IPropertySymbol property => property.Type,
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => model.GetTypeInfo(expression).Type
        };

        return SymbolEqualityComparer.Default.Equals(sourceType, type);
    }

    static bool HasIdentityType(ExpressionSyntax expression, ITypeSymbol type, SemanticModel model)
    {
        var info = model.GetTypeInfo(expression);

        // Target-typed new has no natural Type and uses Roslyn's object-creation conversion,
        // not an identity conversion. Its bound constructor still proves the constructed type.
        var actual = expression is ImplicitObjectCreationExpressionSyntax && model.GetSymbolInfo(expression).Symbol is IMethodSymbol constructor
            ? constructor.ContainingType : info.Type;

        return SymbolEqualityComparer.Default.Equals(actual, type) &&
            (info.ConvertedType is null || SymbolEqualityComparer.Default.Equals(info.ConvertedType, type));
    }

    static bool OnlyEvents(ExpressionSyntax expression, SemanticModel model)
    {
        var unwrapped = MappingSourceReader.Unwrap(expression);
        var type = ResponseValueType(model.GetTypeInfo(unwrapped).Type ?? model.GetTypeInfo(unwrapped).ConvertedType);
        var optional = false;
        var collectionType = false;
        var underlying = type is null ? null : UnderlyingTypes.Of(type, ref optional, ref collectionType);
        return (underlying is not null && EventReader.IsEvent(underlying)) ||
            (unwrapped is ConditionalExpressionSyntax conditional && OnlyEvents(conditional.WhenTrue, model) && OnlyEvents(conditional.WhenFalse, model)) ||
            (unwrapped is CollectionExpressionSyntax collection && collection.Elements.All(element => element is ExpressionElementSyntax item && OnlyEvents(item.Expression, model))) ||
            (unwrapped is ArrayCreationExpressionSyntax { Initializer: { } initializer } && initializer.Expressions.All(item => OnlyEvents(item, model))) ||
            (unwrapped is ImplicitArrayCreationExpressionSyntax array && array.Initializer.Expressions.All(item => OnlyEvents(item, model)));
    }

    static ITypeSymbol? ResponseValueType(ITypeSymbol? type)
    {
        while (type is INamedTypeSymbol named && (named.Is("System.Threading.Tasks.Task`1") || named.Is("System.Threading.Tasks.ValueTask`1")))
        {
            type = named.TypeArguments[0];
        }

        return type is null || type.SpecialType == SpecialType.System_Void ||
            type.Is("System.Threading.Tasks.Task") || type.Is("System.Threading.Tasks.ValueTask") ||
            type.Is("Cratis.Chronicle.EventSequences.IAppendResult") || type.Is("Cratis.Chronicle.EventSequences.AppendResult") ||
            type.AllInterfaces.Any(contract => contract.Is("Cratis.Chronicle.EventSequences.IAppendResult")) ? null : type;
    }

    bool SupportsResponse(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;
        var underlying = UnderlyingTypes.Of(type, ref optional, ref collection);

        return !collection && !underlying.HasAttribute(WellKnownTypeNames.ReadModelAttribute);
    }

    ReturnedParts ReadReturnedParts(INamedTypeSymbol command, ExpressionSyntax returned, SemanticModel model, string location)
    {
        var responses = new List<ExpressionSyntax>();
        var operations = new List<OperationModel>();
        var operationReader = new CommandOperationReader(types, paths, diagnostics);
        var parts = ReturnedExpressions(returned)
            .Select(part => (Part: part, Type: ResponseValueType(model.GetTypeInfo(part).Type ?? model.GetTypeInfo(part).ConvertedType)))
            .Where(_ => _.Type is not null && !OnlyEvents(_.Part, model))
            .ToList();
        responses.AddRange(parts.Where(_ => !IsReturnedOperation(_.Type!)).Select(_ => _.Part));
        var returnedOperations = parts.Where(_ => enabled && IsReturnedOperation(_.Type!));
        operations.AddRange(returnedOperations.SelectMany(_ => operationReader.Read(_.Part, model, command, Sources, location)));

        return new(responses, operations);
    }

    bool ContainsOperation(ITypeSymbol type) => CommandOperationReader.IsOperation(type) || type.Is("Cratis.Arc.Commands.CommandOperations") ||
        (type is INamedTypeSymbol named && named.TypeArguments.Any(ContainsOperation));

    string? SourceOf(ExpressionSyntax expression, SemanticModel model, INamedTypeSymbol command) =>
        MappingSourceReader.ReadPath(expression, model, command) ??
        (model.GetSymbolInfo(MappingSourceReader.Unwrap(expression)).Symbol is ILocalSymbol or IParameterSymbol || expression is BaseObjectCreationExpressionSyntax or InvocationExpressionSyntax
            ? Sources.ReadPath(expression, model) : null);

    List<PropertyMappingModel>? ReadRecord(BaseObjectCreationExpressionSyntax creation, INamedTypeSymbol record, SemanticModel model, INamedTypeSymbol command)
    {
        if (creation.Initializer is not null || !InlineProductionShape.IsSupported(creation, model) || model.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor || creation.ArgumentList is not { } arguments)
        {
            return null;
        }

        var fields = new List<PropertyMappingModel>();
        for (var index = 0; index < arguments.Arguments.Count; index++)
        {
            var argument = arguments.Arguments[index];
            var parameter = argument.NameColon is { } named ? constructor.Parameters.FirstOrDefault(parameter => parameter.Name == named.Name.Identifier.ValueText) : constructor.Parameters.ElementAtOrDefault(index);
            var property = record.DeclaredProperties().FirstOrDefault(property => property.Name == parameter?.Name);
            var source = SourceOf(argument.Expression, model, command);
            if (property?.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is ParameterSyntax) != true ||
                property.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is PropertyDeclarationSyntax) || !SupportsResponse(property.Type) || source?.Contains('.', StringComparison.Ordinal) != false ||
                !IsDirectResponse(argument.Expression, property.Type, model))
            {
                return null;
            }

            fields.Add(new(property.Name, new PropertyPathSource(source)));
        }

        return fields.Count > 0 && fields.Count == record.DeclaredProperties().Count() ? fields : null;
    }

    bool CreatesLegacyGeneratableConcept(ExpressionSyntax expression, ITypeSymbol? type, SemanticModel model, bool explicitGeneration)
    {
        if (explicitGeneration)
        {
            return false;
        }

        return type is not null && CreatesGeneratableConcept(expression, type, model);
    }

    bool CreatesGeneratableConcept(ExpressionSyntax expression, ITypeSymbol type, SemanticModel model) =>
        type is INamedTypeSymbol concept && HasIdentityType(expression, type, model) &&
        !expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax) &&
        new GenerationStrategyReader(models).ReadLocal(MappingSourceReader.Unwrap(expression), concept, model);

    void Report(string code, string message, string location) => diagnostics.Information(code, message, location);

    sealed record ReturnedParts(IReadOnlyList<ExpressionSyntax> Responses, IReadOnlyList<OperationModel> Operations);
}
