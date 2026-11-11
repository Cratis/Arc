// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
public partial class CommandAuthoringReader(SemanticModels models, TypeRegistry types, SourcePaths paths, ScreenplayDiagnostics diagnostics, bool enabled)
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

        if (ReportsDefaultOutputLimits(command, handlers, readsReader, location))
        {
            return result;
        }

        if (SingleHandlerBody(handlers) is not { } single)
        {
            return result;
        }

        return ReadHandlerBody(new(command, single, location, parameters), result);
    }

    /// <summary>Determines whether a type supplies an event source identity.</summary>
    /// <param name="type">The value type.</param>
    /// <returns>Whether the value has the Chronicle event source identity contract.</returns>
    public bool IsEventSourceIdentity(ITypeSymbol? type) => type is not null &&
        (type.Is("Cratis.Chronicle.Events.EventSourceId") || type.FindBase("Cratis.Chronicle.Events.EventSourceId`1") is not null);

    static bool ProvisionStaysInCode(INamedTypeSymbol command, CommandReadsReader reads) => HasProvide(command) && !reads.ProvisionStatesReadsOnly;

    bool ReportsDefaultOutputLimits(INamedTypeSymbol command, IReadOnlyList<IMethodSymbol> handlers, CommandReadsReader readsReader, string location)
    {
        if (enabled)
        {
            return false;
        }

        if (ProvisionStaysInCode(command, readsReader))
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"The provisioning behavior of command '{command.Name}' is not represented in default output; generated values and responses were left in code", location);
            return true;
        }

        if (handlers.Any(handler => ContainsOperation(handler.ReturnType)))
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandOperation, "Returned operations are authoring-only; enable ScreenplayOptions.AuthoringOnlyConstructs to describe readable operations", location);
        }

        return false;
    }

    HandlerBody? SingleHandlerBody(IReadOnlyList<IMethodSymbol> handlers)
    {
        if (handlers is not [var handler] || HandlerBodies.Of(handler).ToArray() is not [var body])
        {
            return null;
        }

        return models.For(body.SyntaxTree) is { } model ? new(handler, body, model) : null;
    }

    CommandAuthoringModel ReadHandlerBody(ReadContext context, CommandAuthoringModel result)
    {
        var parameters = context.Parameters;
        parameters.Bind(context.Command, context.Body, context.Model);
        context.Generated.AddRange(parameters.Generated);
        context.ValidatedGenerated.AddRange(parameters.Validated);
        result = result with
        {
            GeneratedParameters = parameters.HasParameters,
            UnavailableGenerated = parameters.Unavailable
        };
        if (!AddLegacyLocals(context))
        {
            return result;
        }

        if (!context.ReadsStraightReturn)
        {
            return ReportControlFlow(context, result);
        }

        result = result with { Generated = context.Generated, GeneratedWithValidators = context.ValidatedGenerated };
        if (parameters.WrapperIdentifier(context.Returned!, context.Body, context.Model, context.Generated) is { } wrapperIdentity)
        {
            return result with { Identifier = wrapperIdentity };
        }

        return ResponseValueType(context.Handler.ReturnType) is null ? result : ReadResponse(context, result);
    }

    CommandAuthoringModel ReportControlFlow(ReadContext context, CommandAuthoringModel result)
    {
        if (HandlerBodies.YieldsEventSourceId(context.Handler.ReturnType))
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, "The returned response or operations depend on control flow and were left in code", context.Location);
        }

        return result;
    }

    bool AddLegacyLocals(ReadContext context)
    {
        foreach (var variable in context.Parameters.LegacyLocals(context.Body))
        {
            if (!AddLegacyLocal(context, variable))
            {
                return false;
            }
        }

        return true;
    }

    bool AddLegacyLocal(ReadContext context, VariableDeclaratorSyntax variable)
    {
        if (GeneratableLocal(context, variable) is not var (value, local))
        {
            return true;
        }

        if (!IsUnchanged(local, context.Body, context.Model))
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"Generated local '{local.Name}' is written after its initializer or passed by reference; its generation and dependent claims were left in code", context.Location);
            return true;
        }

        if (!context.ReadsStraightReturn)
        {
            return true;
        }

        if (CollidesWithProperty(context, local.Name))
        {
            Report(ScreenplayDiagnosticCodes.UnreadableCommandResponse, $"Generated local '{local.Name}' collides with an emitted command property; generation and dependent claims were left in code", context.Location);
            Sources = new();
            return false;
        }

        AddGeneratedLocal(context, variable, value, local);
        return true;
    }

    (ExpressionSyntax Value, ILocalSymbol Local)? GeneratableLocal(ReadContext context, VariableDeclaratorSyntax variable)
    {
        if (variable.Initializer?.Value is not { } value || context.Model.GetDeclaredSymbol(variable) is not ILocalSymbol local)
        {
            return null;
        }

        return CreatesGeneratableConcept(value, local.Type, context.Model) ? (value, local) : null;
    }

    void AddGeneratedLocal(ReadContext context, VariableDeclaratorSyntax variable, ExpressionSyntax value, ILocalSymbol local)
    {
        // Roslyn annotates inferred reference locals as nullable even when their initializer is required.
        // Keep explicit local annotations, but resolve var from the proven generation expression instead.
        var generatedType = variable.Parent is VariableDeclarationSyntax { Type.IsVar: true }
            ? context.Model.GetTypeInfo(value).Type ?? local.Type
            : local.Type;
        AddGenerated(context, local.Name, generatedType, local.Type);
        Sources.AddGenerated(local, local.Name);
    }

    void AddGenerated(ReadContext context, string name, ITypeSymbol resolvedType, ITypeSymbol validatedType)
    {
        context.Generated.Add(new(name, types.ResolveGenerated(resolvedType)));
        if (ValidatedTypes.Contains(validatedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()))
        {
            context.ValidatedGenerated.Add(name);
        }
    }

    bool CollidesWithProperty(ReadContext context, string name) =>
        context.Command.DeclaredProperties().Select(property => property.Name).Concat(context.Generated.Select(property => property.Name))
            .Any(existing => _naming.ToPropertyName(existing) == _naming.ToPropertyName(name));

    void Report(string code, string message, string location) => diagnostics.Information(code, message, location);

    sealed record HandlerBody(IMethodSymbol Handler, SyntaxNode Body, SemanticModel Model);

    sealed class ReadContext(INamedTypeSymbol command, HandlerBody handlerBody, string location, GeneratedParameterReader parameters)
    {
        public INamedTypeSymbol Command { get; } = command;

        public IMethodSymbol Handler { get; } = handlerBody.Handler;

        public SyntaxNode Body { get; } = handlerBody.Body;

        public SemanticModel Model { get; } = handlerBody.Model;

        public string Location { get; } = location;

        public GeneratedParameterReader Parameters { get; } = parameters;

        public ExpressionSyntax? Returned { get; } = ReturnedBy(handlerBody.Body);

        public bool ReadsStraightReturn { get; } = ReturnedBy(handlerBody.Body) is not null && IsStraight(handlerBody.Body, handlerBody.Model);

        public List<PropertyModel> Generated { get; } = [];

        public List<string> ValidatedGenerated { get; } = [];
    }
}
