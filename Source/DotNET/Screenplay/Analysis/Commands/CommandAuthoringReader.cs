// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
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
    /// <summary>Gets the proven sources of the command most recently read.</summary>
    public AuthoringSources Sources { get; private set; } = new();

    /// <summary>
    /// Gets the types with concept validators, including unreadable rules.
    /// </summary>
    public IReadOnlySet<string> ValidatedTypes { get; init; } = new HashSet<string>();

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
        var reads = new CommandReadsReader(models, types, diagnostics, enabled).Read(command, identifier, Sources, location);
        var result = new CommandAuthoringModel { Reads = reads.Reads, Requirements = reads.Requirements };
        var route = new CommandRouteReader(models, types, diagnostics, enabled).Read(command, identifier, location);
        result = result with { Route = route };

        if (!enabled)
        {
            if (command.GetMembers("Provide").OfType<IMethodSymbol>().Any())
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

        var returned = body is BlockSyntax block
            ? block.Statements.LastOrDefault() is ReturnStatementSyntax statement ? statement.Expression : null
            : body as ExpressionSyntax;
        var straight = body is not BlockSyntax statements || (statements.Statements.All(statement => statement is LocalDeclarationStatementSyntax or ReturnStatementSyntax) &&
            statements.Statements.OfType<ReturnStatementSyntax>().Count() == 1);
        var generated = new List<PropertyModel>();
        var validatedGenerated = new List<string>();
        foreach (var variable in body is BlockSyntax straightBody ? straightBody.Statements.OfType<LocalDeclarationStatementSyntax>().SelectMany(statement => statement.Declaration.Variables).ToList() : [])
        {
            if (variable.Initializer?.Value is not { } value || model.GetDeclaredSymbol(variable) is not ILocalSymbol local)
            {
                continue;
            }

            if (CreatesUuidConcept(value, local.Type, model))
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

                if (command.DeclaredProperties().Any(property => string.Equals(property.Name, local.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"Generated local '{local.Name}' collides with a command property and was left out", location);
                    continue;
                }

                // Roslyn annotates inferred reference locals as nullable even when their initializer is required.
                // Keep explicit local annotations, but resolve var from the proven UUID creation instead.
                var generatedType = variable.Parent is VariableDeclarationSyntax { Type.IsVar: true }
                    ? model.GetTypeInfo(value).Type ?? local.Type
                    : local.Type;
                generated.Add(new(local.Name, types.Resolve(generatedType)));
                if (ValidatedTypes.Contains(local.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()))
                {
                    validatedGenerated.Add(local.Name);
                }
                Sources.Add(local, local.Name);
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
        if (ResponseValueType(handler.ReturnType) is null)
        {
            return result;
        }

        var parts = returned is TupleExpressionSyntax tuple ? tuple.Arguments.Select(argument => argument.Expression).ToArray() : [returned];
        var responses = new List<ExpressionSyntax>();
        var operations = new List<OperationModel>();
        var operationReader = new CommandOperationReader(types, paths, diagnostics);
        foreach (var part in parts)
        {
            var type = ResponseValueType(model.GetTypeInfo(part).Type ?? model.GetTypeInfo(part).ConvertedType);
            if (type is null || OnlyEvents(part, model))
            {
                continue;
            }

            if (CommandOperationReader.IsOperation(type) || type.Is("Cratis.Arc.Commands.CommandOperations"))
            {
                if (enabled)
                {
                    operations.AddRange(operationReader.Read(part, model, command, Sources, location));
                }
                continue;
            }

            responses.Add(part);
        }

        result = result with { Operations = operations };
        if (responses.Count > 1)
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "Several response values are returned without a response record; no response was inferred", location);
            return result;
        }

        if (responses is not [var response])
        {
            return result;
        }

        var responseType = ResponseValueType(model.GetTypeInfo(response).Type ?? model.GetTypeInfo(response).ConvertedType);
        if (responseType is not null && CreatesUuidConcept(response, responseType, model))
        {
            var name = char.ToLowerInvariant(responseType.Name[0]) + responseType.Name[1..];
            if (!command.DeclaredProperties().Any(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) &&
                !generated.Exists(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                generated.Add(new(name, types.Resolve(responseType)));
                if (ValidatedTypes.Contains(responseType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()))
                {
                    validatedGenerated.Add(name);
                }
                Sources.AddExpression(response, name);
            }
        }

        result = result with { Generated = generated, GeneratedWithValidators = validatedGenerated };
        var source = SourceOf(response, model, command);
        if (source?.Contains('.', StringComparison.Ordinal) == false && responseType is not null && SupportsResponse(responseType) &&
            IsDirectResponse(response, responseType, model))
        {
            var generatedIdentifier = generated.Exists(property => property.Name == source) && IsEventSourceIdentity(responseType) &&
                !AggregateRootBehaviors.ReachedFrom(body, model).Any();
            return result with { Response = source, Identifier = generatedIdentifier ? source : null };
        }

        if (response is BaseObjectCreationExpressionSyntax creation && model.GetTypeInfo(creation).Type is INamedTypeSymbol { IsRecord: true } record &&
            record.FindBase(WellKnownTypeNames.ConceptAs) is null && HasIdentityType(creation, record, model) &&
            (response != returned || SymbolEqualityComparer.Default.Equals(record, ResponseValueType(handler.ReturnType))) &&
            ReadRecord(creation, record, model, command) is { } fields)
        {
            return result with { ResponseFields = fields };
        }

        Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "The response is not a direct command property, generated UUID concept, or fully readable response record and was left in code", location);
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
        if (handler.ContainingType.GetMembers("Provide").OfType<IMethodSymbol>().Any() ||
            HandlerBodies.Of(handler).ToArray() is not [var body])
        {
            return false;
        }

        if (body is BlockSyntax { Statements.Count: 0 })
        {
            return true;
        }

        if (ContainsOperation(handler.ReturnType) || authoring is not { Response: not null } and not { ResponseFields.Count: > 0 })
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

    bool CreatesFromNewGuid(ExpressionSyntax expression, SemanticModel model) =>
        MappingSourceReader.Unwrap(expression) is BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation &&
        MappingSourceReader.Unwrap(argument.Expression) is InvocationExpressionSyntax invocation &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: true, Parameters.Length: 0 } factory && factory.Name == "NewGuid" && factory.ContainingType.Is("System.Guid") &&
        model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor && ForwardsUuid(constructor, new(SymbolEqualityComparer.Default));

    bool ForwardsUuid(IMethodSymbol constructor, HashSet<IMethodSymbol> visited)
    {
        if (!visited.Add(constructor) || constructor.Parameters is not [var parameter] || !parameter.Type.Is("System.Guid"))
        {
            return false;
        }

        if ((constructor.ContainingType.Is(WellKnownTypeNames.ConceptAs) || constructor.ContainingType.Is("Cratis.Chronicle.Events.EventSourceId`1")) &&
            constructor.ContainingAssembly.Name.StartsWith("Cratis.", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var reference in constructor.DeclaringSyntaxReferences)
        {
            var declaration = reference.GetSyntax();
            var initializer = declaration switch
            {
                RecordDeclarationSyntax record => (SyntaxNode?)record.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault(),
                ConstructorDeclarationSyntax { Body: null or { Statements.Count: 0 }, ExpressionBody: null } explicitConstructor => explicitConstructor.Initializer,
                _ => null
            };
            var arguments = initializer switch
            {
                PrimaryConstructorBaseTypeSyntax primary => primary.ArgumentList,
                ConstructorInitializerSyntax explicitInitializer => explicitInitializer.ArgumentList,
                _ => null
            };
            if (arguments?.Arguments is [var argument] && models.For(initializer!.SyntaxTree) is { } model &&
                model.GetSymbolInfo(MappingSourceReader.Unwrap(argument.Expression)).Symbol is IParameterSymbol forwarded &&
                !argument.Expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax) &&
                model.GetConversion(argument.Expression).IsIdentity &&
                SymbolEqualityComparer.Default.Equals(forwarded, parameter) && model.GetSymbolInfo(initializer).Symbol is IMethodSymbol target && ForwardsUuid(target, visited))
            {
                return true;
            }
        }

        return false;
    }

    bool SupportsResponse(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;
        var underlying = UnderlyingTypes.Of(type, ref optional, ref collection);

        return !collection && !underlying.HasAttribute(WellKnownTypeNames.ReadModelAttribute);
    }

    bool ContainsOperation(ITypeSymbol type) => CommandOperationReader.IsOperation(type) || type.Is("Cratis.Arc.Commands.CommandOperations") ||
        (type is INamedTypeSymbol named && named.TypeArguments.Any(ContainsOperation));

    string? SourceOf(ExpressionSyntax expression, SemanticModel model, INamedTypeSymbol command) =>
        MappingSourceReader.ReadPath(expression, model, command) ??
        (model.GetSymbolInfo(MappingSourceReader.Unwrap(expression)).Symbol is ILocalSymbol || expression is BaseObjectCreationExpressionSyntax or InvocationExpressionSyntax
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

    bool CreatesUuidConcept(ExpressionSyntax expression, ITypeSymbol type, SemanticModel model)
    {
        if (type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments is not [var backing] || !backing.Is("System.Guid") ||
            expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax) ||
            !HasIdentityType(expression, type, model))
        {
            return false;
        }

        expression = MappingSourceReader.Unwrap(expression);
        if (CreatesFromNewGuid(expression, model))
        {
            return true;
        }

        if (expression is not InvocationExpressionSyntax invocation ||
            model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { IsStatic: true, Parameters.Length: 0, Name: "New" } factory ||
            !SymbolEqualityComparer.Default.Equals(factory.ReturnType, type) ||
            !(SymbolEqualityComparer.Default.Equals(factory.ContainingType, type) ||
                ((factory.ContainingType.Is("Cratis.Chronicle.Events.EventSourceId`1") || factory.ContainingType.Is(WellKnownTypeNames.ConceptAs)) &&
                    factory.ContainingAssembly.Name.StartsWith("Cratis.", StringComparison.Ordinal) &&
                    SymbolEqualityComparer.Default.Equals(type.FindBase(factory.ContainingType.FullMetadataName()), factory.ContainingType))))
        {
            return false;
        }

        // A name is not a generation contract. Only a source body proving fresh UUID creation is admitted.
        return factory.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<MethodDeclarationSyntax>().Any(declaration =>
        {
            var returned = declaration.ExpressionBody?.Expression ??
                (declaration.Body?.Statements is [ReturnStatementSyntax statement] ? statement.Expression : null);

            return returned is not null && models.For(declaration.SyntaxTree) is { } factoryModel &&
                !returned.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax) &&
                HasIdentityType(returned, type, factoryModel) && CreatesFromNewGuid(returned, factoryModel);
        });
    }

    void Report(string code, string message, string location) => diagnostics.Information(code, message, location);
}
