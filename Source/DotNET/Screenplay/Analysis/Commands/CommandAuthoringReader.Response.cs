// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

public partial class CommandAuthoringReader
{
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

    static bool IsStraight(SyntaxNode body, SemanticModel model) => ReturnedEventList.Read(body, model) is not null || body is not BlockSyntax block ||
        (block.Statements.All(statement => statement is LocalDeclarationStatementSyntax or ReturnStatementSyntax) && block.Statements.OfType<ReturnStatementSyntax>().Count() == 1);

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

        return IsAppendedEvent(UnderlyingResponseType(unwrapped, model)) || OnlyEventElements(unwrapped, model);
    }

    static bool OnlyEventElements(ExpressionSyntax unwrapped, SemanticModel model) => unwrapped switch
    {
        ConditionalExpressionSyntax conditional => OnlyEvents(conditional.WhenTrue, model) && OnlyEvents(conditional.WhenFalse, model),
        CollectionExpressionSyntax collection => collection.Elements.All(element => element is ExpressionElementSyntax item && OnlyEvents(item.Expression, model)),
        _ => OnlyEventArrayItems(unwrapped, model)
    };

    static bool OnlyEventArrayItems(ExpressionSyntax unwrapped, SemanticModel model) => unwrapped switch
    {
        ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions.All(item => OnlyEvents(item, model)),
        ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions.All(item => OnlyEvents(item, model)),
        _ => false
    };

    static ITypeSymbol? UnderlyingResponseType(ExpressionSyntax unwrapped, SemanticModel model)
    {
        var type = ResponseValueType(model.GetTypeInfo(unwrapped).Type ?? model.GetTypeInfo(unwrapped).ConvertedType);
        var optional = false;
        var collectionType = false;

        return type is null ? null : UnderlyingTypes.Of(type, ref optional, ref collectionType);
    }

    static bool IsAppendedEvent(ITypeSymbol? type) => type is not null && (EventReader.IsEvent(type) || type.Is(EventWrapperType.MetadataName));

    static ITypeSymbol? ResponseValueType(ITypeSymbol? type)
    {
        type = UnwrapTasks(type);

        return IsNoResponseType(type) ? null : type;
    }

    static ITypeSymbol? UnwrapTasks(ITypeSymbol? type)
    {
        while (type is INamedTypeSymbol named && IsTaskOfValue(named))
        {
            type = named.TypeArguments[0];
        }

        return type;
    }

    static bool IsTaskOfValue(INamedTypeSymbol type) => type.Is("System.Threading.Tasks.Task`1") || type.Is("System.Threading.Tasks.ValueTask`1");

    static bool IsNoResponseType(ITypeSymbol? type) =>
        type is null || type.SpecialType == SpecialType.System_Void || type.Is("System.Threading.Tasks.Task") || type.Is("System.Threading.Tasks.ValueTask") || IsAppendResult(type);

    static bool IsAppendResult(ITypeSymbol type) =>
        type.Is("Cratis.Chronicle.EventSequences.IAppendResult") || type.Is("Cratis.Chronicle.EventSequences.AppendResult") ||
        type.AllInterfaces.Any(contract => contract.Is("Cratis.Chronicle.EventSequences.IAppendResult"));

    static IEnumerable<ExpressionSyntax> ResponseExpressions(ExpressionSyntax returned, SemanticModel model)
    {
        if (ReturnedProductionShape.TupleOf(returned, model) is not { } tuple)
        {
            return ReturnedExpressions(returned);
        }

        return tuple.Arguments.Select(argument => argument.Expression)
            .Where(expression => !ReturnedProductionShape.IsEffect(model.GetTypeInfo(expression).ConvertedType ?? model.GetTypeInfo(expression).Type));
    }

    CommandAuthoringModel ReadResponse(ReadContext context, CommandAuthoringModel result)
    {
        var parts = ReadReturnedParts(context.Command, context.Returned!, context.Model, context.Location);
        result = result with
        {
            Operations = parts.Operations,
            Identifier = context.Parameters.TupleWrapperIdentifier(context.Returned!, context.Body, context.Model, context.Generated)
        };
        if (parts.Responses is [var response])
        {
            return ReadSingleResponse(context, result, response);
        }

        if (parts.Responses.Count > 1)
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "Several response values are returned without a response record; no response was inferred", context.Location);
        }

        return result;
    }

    CommandAuthoringModel ReadSingleResponse(ReadContext context, CommandAuthoringModel result, ExpressionSyntax response)
    {
        if (Sources.UnavailableIn(response, context.Model) is { } unavailable)
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"The response depends on unavailable generated input '{unavailable.Name}' ({unavailable.Failure}) and was left in code", context.Location);
            return result;
        }

        var responseType = ResponseValueType(context.Model.GetTypeInfo(response).Type ?? context.Model.GetTypeInfo(response).ConvertedType);
        if (CreatesLegacyGeneratableConcept(response, responseType, context.Model, context.Parameters.HasParameters) && !AddGeneratedResponse(context, response, responseType!))
        {
            return result with { Generated = [], GeneratedWithValidators = [], Operations = [] };
        }

        result = result with { Generated = context.Generated, GeneratedWithValidators = context.ValidatedGenerated };

        return ReadDirectResponse(context, result, response, responseType) ??
            ReadResponseRecord(context, result, response) ??
            ReportUnreadableResponse(context, result);
    }

    CommandAuthoringModel ReportUnreadableResponse(ReadContext context, CommandAuthoringModel result)
    {
        Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "The response is not a direct command property, generated concept, or fully readable response record and was left in code", context.Location);
        return result;
    }

    bool AddGeneratedResponse(ReadContext context, ExpressionSyntax response, ITypeSymbol responseType)
    {
        var name = char.ToLowerInvariant(responseType.Name[0]) + responseType.Name[1..];
        if (CollidesWithProperty(context, name))
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"Generated response '{name}' collides with an emitted command property; generation and dependent claims were left in code", context.Location);
            Sources = new();
            return false;
        }

        AddGenerated(context, name, responseType, responseType);
        Sources.AddExpression(response, name);
        return true;
    }

    CommandAuthoringModel? ReadDirectResponse(ReadContext context, CommandAuthoringModel result, ExpressionSyntax response, ITypeSymbol? responseType)
    {
        var source = SourceOf(response, context.Model, context.Command);
        if (source is null || !IsDirectResponseSource(context, response, responseType, source))
        {
            return null;
        }

        var generatedIdentifier = context.Generated.Exists(property => property.Name == source) && IsEventSourceIdentity(responseType) &&
            !AggregateRootBehaviors.ReachedFrom(context.Body, context.Model).Any();

        return result with { Response = source, Identifier = generatedIdentifier ? source : result.Identifier, ResponseType = ResponseTypes.NameOf(responseType!) };
    }

    bool IsDirectResponseSource(ReadContext context, ExpressionSyntax response, ITypeSymbol? responseType, string source) =>
        !source.Contains('.', StringComparison.Ordinal) && responseType is not null && SupportsResponse(responseType) &&
        IsDirectResponse(response, responseType, context.Model);

    CommandAuthoringModel? ReadResponseRecord(ReadContext context, CommandAuthoringModel result, ExpressionSyntax response)
    {
        if (response is not BaseObjectCreationExpressionSyntax creation || context.Model.GetTypeInfo(creation).Type is not INamedTypeSymbol { IsRecord: true } record)
        {
            return null;
        }

        return IsReadableResponseRecord(context, response, creation, record) && ReadRecord(creation, record, context.Model, context.Command) is { } fields
            ? result with { ResponseFields = fields, ResponseType = ResponseTypes.NameOf(record) }
            : null;
    }

    bool IsReadableResponseRecord(ReadContext context, ExpressionSyntax response, BaseObjectCreationExpressionSyntax creation, INamedTypeSymbol record) =>
        record.FindBase(WellKnownTypeNames.ConceptAs) is null && HasIdentityType(creation, record, context.Model) &&
        (response != context.Returned || SymbolEqualityComparer.Default.Equals(record, ResponseValueType(context.Handler.ReturnType)));

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
        var parts = ResponseExpressions(returned, model)
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

    sealed record ReturnedParts(IReadOnlyList<ExpressionSyntax> Responses, IReadOnlyList<OperationModel> Operations);
}
