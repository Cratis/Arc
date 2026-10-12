// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Recovers explicit generated parameters before the legacy local-value heuristics run.
/// </summary>
/// <param name="models">The models resolving generation strategies.</param>
/// <param name="types">The application's type registry.</param>
/// <param name="diagnostics">Where unrecovered generation is reported.</param>
internal class GeneratedParameterReader(SemanticModels models, TypeRegistry types, ScreenplayDiagnostics diagnostics)
{
    readonly ScreenplayNaming _naming = new();
    readonly List<PropertyModel> _generated = [];
    readonly List<string> _validated = [];
    List<IParameterSymbol> _parameters = [];
    string _location = string.Empty;

    /// <summary>
    /// Gets the proven sources to register parameters in.
    /// </summary>
    public AuthoringSources Sources { get; init; } = new();

    /// <summary>
    /// Gets the types with concept validators.
    /// </summary>
    public IReadOnlySet<string> ValidatedTypes { get; init; } = new HashSet<string>();

    /// <summary>
    /// Gets whether the handler declares explicit generation.
    /// </summary>
    public bool HasParameters => _parameters.Count > 0;

    /// <summary>
    /// Gets the recovered generated properties.
    /// </summary>
    public IReadOnlyList<PropertyModel> Generated => _generated;

    /// <summary>
    /// Gets the generated names whose concepts have validators.
    /// </summary>
    public IReadOnlyList<string> Validated => _validated;

    /// <summary>
    /// Reads every declared generation strategy, reporting all refusals.
    /// </summary>
    /// <param name="handlers">The command handlers.</param>
    /// <param name="location">The command's diagnostic location.</param>
    /// <returns>Whether every strategy is recoverable.</returns>
    public bool ReadStrategies(IReadOnlyList<IMethodSymbol> handlers, string location)
    {
        _location = location;
        _parameters = handlers.SelectMany(handler => handler.Parameters).Where(parameter => parameter.HasAttribute(WellKnownTypeNames.GeneratedValueAttribute)).ToList();
        var strategies = new GenerationStrategyReader(models);
        var failures = _parameters.Select(parameter => (Parameter: parameter, Failure: strategies.Failure(parameter))).Where(item => item.Failure is not null).ToList();
        foreach (var (parameter, failure) in failures)
        {
            ReportStrategy(parameter, failure!);
        }

        return failures.Count == 0;
    }

    /// <summary>
    /// Binds unchanged generated parameters to unique emitted property names.
    /// </summary>
    /// <param name="command">The command declaring the parameters.</param>
    /// <param name="body">The handler body.</param>
    /// <param name="model">The model resolving parameter uses.</param>
    /// <returns>Whether all parameters can be bound faithfully.</returns>
    public bool Bind(INamedTypeSymbol command, SyntaxNode body, SemanticModel model)
    {
        foreach (var parameter in _parameters)
        {
            var name = AttributeOf(parameter).ConstructorArguments.FirstOrDefault().Value as string ?? parameter.Name;
            if (!CommandAuthoringReader.IsUnchanged(parameter, body, model) || Collides(name, command))
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UnreadableCommandResponse,
                    $"Generated parameter '{name}' is written, passed by reference, or collides with an emitted command property; generation and dependent claims were left in code",
                    _location);
                return false;
            }

            _generated.Add(new(name, types.Resolve(parameter.Type)));
            if (ValidatedTypes.Contains(parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()))
            {
                _validated.Add(name);
            }
            Sources.Add(parameter, name);
        }

        return true;
    }

    /// <summary>
    /// Selects local candidates only when no explicit generation contract is present.
    /// </summary>
    /// <param name="body">The handler body.</param>
    /// <returns>The locals eligible for the legacy heuristic.</returns>
    public IEnumerable<VariableDeclaratorSyntax> LegacyLocals(SyntaxNode body) => HasParameters ? [] : LocalsOf(body);

    /// <summary>
    /// Proves the identity of a directly returned EventForEventSourceId wrapper.
    /// </summary>
    /// <param name="returned">The directly returned expression.</param>
    /// <param name="body">The handler body.</param>
    /// <param name="model">The model resolving the wrapper.</param>
    /// <param name="generated">All proven generated properties, including legacy locals.</param>
    /// <returns>The wrapper's generated identity, or null when unproven.</returns>
    public string? WrapperIdentifier(ExpressionSyntax returned, SyntaxNode body, SemanticModel model, IReadOnlyList<PropertyModel> generated)
    {
        if (returned is not BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [var destination, ..] } wrapper ||
            !model.GetTypeInfo(wrapper).Type.Is("Cratis.Chronicle.EventSequences.EventForEventSourceId"))
        {
            return null;
        }
        var identity = Sources.ReadPath(destination.Expression, model);
        if (identity is null || !generated.Any(property => property.Name == identity))
        {
            return null;
        }

        return AggregateRootBehaviors.ReachedFrom(body, model).Any() ? null : identity;
    }

    static AttributeData AttributeOf(IParameterSymbol parameter) => parameter.GetAttributes().First(attribute => attribute.AttributeClass.Is(WellKnownTypeNames.GeneratedValueAttribute));

    static List<VariableDeclaratorSyntax> LocalsOf(SyntaxNode body) => body is BlockSyntax block
        ? block.Statements.OfType<LocalDeclarationStatementSyntax>().SelectMany(statement => statement.Declaration.Variables).ToList()
        : [];

    void ReportStrategy(IParameterSymbol parameter, string failure)
    {
        var syntax = AttributeOf(parameter).ApplicationSyntaxReference?.GetSyntax();
        var location = syntax is null ? $"{_location}.{parameter.Name}" : $"{syntax.SyntaxTree.FilePath}:{syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
        diagnostics.Information(
            ScreenplayDiagnosticCodes.GenerationNotRecoverable,
            $"[GeneratedValue] parameter '{parameter.Name}' of '{parameter.Type.Name}': {failure}; generation remains in code. Other strategies and rules require Cratis/Screenplay#616",
            location);
    }

    bool Collides(string name, INamedTypeSymbol command) => command.DeclaredProperties().Select(property => property.Name).Concat(_generated.Select(property => property.Name))
        .Any(existing => _naming.ToPropertyName(existing) == _naming.ToPropertyName(name));
}
