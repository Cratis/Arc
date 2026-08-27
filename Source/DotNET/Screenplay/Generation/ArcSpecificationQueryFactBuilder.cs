// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Queries;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

using static Cratis.Arc.Screenplay.Generation.ArcSpecificationFacts;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Converts one exact generated query specification into an atomic neutral fact contribution.
/// </summary>
/// <param name="context">The analyzed application projects.</param>
/// <param name="scenarioProject">The project declaring the scenario.</param>
/// <param name="adapter">The adapter identity.</param>
/// <param name="options">The host placement options.</param>
/// <param name="sourceStructures">The fixed source-structure snapshot.</param>
/// <param name="diagnostics">The diagnostics to append.</param>
internal sealed class ArcSpecificationQueryFactBuilder(
    DotNetAnalysisContext context,
    DotNetProjectCompilation scenarioProject,
    AdapterIdentity adapter,
    DotNetAdapterOptions options,
    DotNetSourceStructureSnapshot sourceStructures,
    List<GenerationDiagnostic> diagnostics)
{
    readonly List<GenerationFact> _facts = [];
    readonly ArcSpecificationEvidence _sourceEvidence = new(context, scenarioProject, adapter, diagnostics);
    readonly ScreenplayNaming _naming = new();

    ArcSpecificationArtifactFacts ArtifactFacts => new(context, scenarioProject, adapter, sourceStructures, _facts);

    /// <summary>
    /// Adds one query scenario only when every artifact, relationship, ordered value, placement, and source is exact.
    /// </summary>
    /// <param name="name">The recovered scenario name.</param>
    /// <param name="evidence">The exact query scenario evidence.</param>
    /// <returns>The complete candidate, or <see langword="null"/> when the scenario cannot be proven exactly.</returns>
    public ArcSpecificationFactCandidate? Build(string name, SpecificationQueryEvidence evidence)
    {
        var scenarioSubject = scenarioProject.SubjectForType(evidence.SourceType);
        var inputParameters = evidence.Query.Parameters.Where(QueryReader.IsInput).ToArray();
        var resultProperties = evidence.ReadModel.DeclaredProperties().ToArray();
        if (inputParameters.Length != evidence.Arguments.Count ||
            inputParameters.Zip(evidence.Arguments).Any(pair => !string.Equals(pair.First.Name, pair.Second.Property, StringComparison.Ordinal)) ||
            resultProperties.Length != evidence.Result.Count ||
            resultProperties.Zip(evidence.Result).Any(pair => !string.Equals(pair.First.Name, pair.Second.Property, StringComparison.Ordinal)))
        {
            _sourceEvidence.Block(name, evidence, "the query arguments or result values lost their exact formal declaration order");
            return null;
        }

        if (!NamesAreUnique(inputParameters.Select(_ => _.Name)) ||
            !NamesAreUnique(resultProperties.Select(_ => _.Name)))
        {
            _sourceEvidence.Block(name, evidence, "two query argument or result paths normalize to the same semantic property name");
            return null;
        }

        if (inputParameters is not [var input] ||
            !IsNonNullableScalar(input.Type) ||
            resultProperties.Count(property =>
                string.Equals(_naming.ToPropertyName(property.Name), _naming.ToPropertyName(input.Name), StringComparison.Ordinal) &&
                SymbolEqualityComparer.IncludeNullability.Equals(property.Type, input.Type) &&
                IsNonNullableScalar(property.Type)) != 1)
        {
            _sourceEvidence.Block(name, evidence, "the required query input has no unique non-nullable scalar read-model key with the same normalized name and exact type");
            return null;
        }

        var queryKey = ArtifactFacts.Query(evidence.Query, _naming.ToPropertyName);
        var readModelKey = ArtifactFacts.Artifact(
            evidence.ReadModel,
            ArtifactKind.ReadModel,
            evidence.ExpectedSource,
            _naming.ToPropertyName,
            type => QueryTypeReference(type, context));
        if (queryKey is null || readModelKey is null)
        {
            _sourceEvidence.Block(name, evidence, "the query or read model has no unique analyzed source identity");
            return null;
        }

        var stepKey = StepKey(scenarioSubject, 0);
        var valueFacts = new List<SpecificationValueFact>();
        var valueKeys = new List<SpecificationValueKey>();
        foreach (var (parameter, value) in inputParameters.Zip(evidence.Arguments))
        {
            if (!TryAddValue(name, evidence, stepKey, ["arguments", _naming.ToPropertyName(parameter.Name)], parameter.Type, value, valueFacts, valueKeys))
            {
                return null;
            }
        }

        foreach (var (property, value) in resultProperties.Zip(evidence.Result))
        {
            if (!TryAddValue(name, evidence, stepKey, ["result", "0", _naming.ToPropertyName(property.Name)], property.Type, value, valueFacts, valueKeys))
            {
                return null;
            }
        }

        var querySource = evidence.Query.Locations
            .Where(_ => _.IsInSource)
            .OrderBy(_ => _.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(_ => _.SourceSpan.Start)
            .FirstOrDefault();
        if (querySource is null)
        {
            _sourceEvidence.Block(name, evidence, "the matched application query has no exact authored declaration");
            return null;
        }

        _facts.Add(new RelationshipFact
        {
            Id = FactId("relationship", queryKey.Subject, $"Returns:{readModelKey.Subject.Value}"),
            Subject = queryKey.Subject,
            Evidence = _sourceEvidence.For(querySource, "The exact application query return type declares this relationship"),
            Definition = new RelationshipDefinition
            {
                Key = new RelationshipKey
                {
                    Kind = RelationshipKind.Returns,
                    Source = queryKey.Subject,
                    Target = readModelKey.Subject
                },
                IsCollection = false,
                IsOptional = evidence.IsOptional
            }
        });

        var step = new SpecificationStepFact
        {
            Id = FactId("step", scenarioSubject, "0"),
            Subject = StepSubject(scenarioSubject, 0),
            Evidence = _sourceEvidence.For(evidence.QueryInvocationSource, "The exact awaited assignment performs this read"),
            Definition = new SpecificationStepDefinition
            {
                Key = stepKey,
                Phase = SpecificationStepPhase.Then,
                Kind = SpecificationStepKind.Read,
                Artifact = queryKey,
                Values = valueKeys
            }
        };
        _facts.Add(new SpecificationScenarioFact
        {
            Id = FactId("scenario", scenarioSubject),
            Subject = scenarioSubject,
            Evidence = _sourceEvidence.For(evidence.Source, "The authored type is an exact generated query specification"),
            Definition = new SpecificationScenarioDefinition
            {
                Key = new() { Scenario = scenarioSubject },
                Name = name,
                TargetArtifact = queryKey,
                Steps = [stepKey]
            }
        });
        _facts.Add(step);
        _facts.AddRange(valueFacts);

        var readModelPlacement = ArtifactFacts.PlacementRequest(readModelKey, GenerationSliceKind.StateView, options.SourceStructurePolicy);
        var queryPlacement = ArtifactFacts.PlacementRequest(queryKey, GenerationSliceKind.StateView, options.SourceStructurePolicy, readModelKey.Subject);
        if (readModelPlacement is null || queryPlacement is null)
        {
            _sourceEvidence.Block(name, evidence, "the query or read model has no exact shared source structure");
            return null;
        }

        return new([queryPlacement, readModelPlacement], _facts);
    }

    bool TryAddValue(
        string name,
        SpecificationQueryEvidence evidence,
        SpecificationStepKey step,
        IReadOnlyList<string> path,
        ITypeSymbol type,
        PropertyMappingModel value,
        List<SpecificationValueFact> facts,
        List<SpecificationValueKey> keys)
    {
        if (!ArcSpecificationValueFacts.TryAddQueryAt(
                context,
                _sourceEvidence.For,
                evidence.ValueEvidence,
                step,
                path,
                type,
                value,
                facts,
                out var key,
                out var reason))
        {
            _sourceEvidence.Block(name, evidence, reason!);
            return false;
        }

        keys.Add(key!);
        return true;
    }

    bool NamesAreUnique(IEnumerable<string> names)
    {
        var normalized = names.Select(_naming.ToPropertyName).ToArray();
        return normalized.Distinct(StringComparer.Ordinal).Count() == normalized.Length;
    }

    bool IsNonNullableScalar(ITypeSymbol type) =>
        type.NullableAnnotation != NullableAnnotation.Annotated &&
        type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } &&
        CollectionElements.ElementOf(type) is null;
}
