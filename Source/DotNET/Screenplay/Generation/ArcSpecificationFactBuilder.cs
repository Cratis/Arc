// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

using static Cratis.Arc.Screenplay.Generation.ArcSpecificationFacts;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Converts one exact legacy Arc specification model into an atomic neutral fact contribution.
/// </summary>
/// <param name="context">The analyzed application projects.</param>
/// <param name="scenarioProject">The project declaring the scenario.</param>
/// <param name="adapter">The adapter identity.</param>
/// <param name="options">The host placement options.</param>
/// <param name="sourceStructures">The fixed source-structure snapshot.</param>
/// <param name="diagnostics">The diagnostics to append.</param>
internal sealed class ArcSpecificationFactBuilder(
    DotNetAnalysisContext context,
    DotNetProjectCompilation scenarioProject,
    AdapterIdentity adapter,
    DotNetAdapterOptions options,
    DotNetSourceStructureSnapshot sourceStructures,
    List<GenerationDiagnostic> diagnostics)
{
    readonly List<GenerationFact> _facts = [];
    readonly ArcSpecificationEvidence _sourceEvidence = new(context, scenarioProject, adapter, diagnostics);

    ArcSpecificationArtifactFacts ArtifactFacts => new(context, scenarioProject, adapter, sourceStructures, _facts);

    /// <summary>
    /// Adds one scenario only when every required step, value, artifact, and source location is exact.
    /// </summary>
    /// <param name="specification">The recovered legacy specification.</param>
    /// <param name="evidence">The exact source evidence.</param>
    /// <param name="target">The exact command or read-model target.</param>
    /// <returns>The complete candidate, or <see langword="null"/> when the scenario cannot be proven exactly.</returns>
    public ArcSpecificationFactCandidate? Build(
        SpecificationModel specification,
        SpecificationScenarioEvidence evidence,
        INamedTypeSymbol target)
    {
        if (evidence.Blockers.Count > 0 || HasUnrepresentedEventPredicate(specification, evidence))
        {
            _sourceEvidence.Block(specification, evidence, "the existing Arc analyzer cannot prove every authored step and value exactly");
            return null;
        }

        var targetsStateView = SpecificationMembers.ReadModelOf(SpecificationMembers.StepsOf(evidence.SourceType)) is not null;
        var targetKind = targetsStateView ? ArtifactKind.ReadModel : ArtifactKind.Command;
        var sliceKind = targetsStateView ? GenerationSliceKind.StateView : GenerationSliceKind.StateChange;
        var scenarioSubject = scenarioProject.SubjectForType(evidence.SourceType);
        var targetKey = ArtifactFacts.Artifact(target, targetKind, evidence.Source);
        if (targetKey is null)
        {
            _sourceEvidence.Block(specification, evidence, "the target artifact has no unique analyzed source identity");
            return null;
        }

        var stepFacts = new List<SpecificationStepFact>();
        var valueFacts = new List<SpecificationValueFact>();
        var stepIndex = 0;
        foreach (var state in specification.Given)
        {
            if (!TryAddState(specification, evidence, scenarioSubject, state, SpecificationStepPhase.Given, stepIndex++, stepFacts, valueFacts))
            {
                return null;
            }
        }

        if (!targetsStateView &&
            (specification.When is null ||
             !TryAddState(specification, evidence, scenarioSubject, specification.When, SpecificationStepPhase.When, stepIndex++, stepFacts, valueFacts)))
        {
            return null;
        }

        foreach (var state in specification.Then)
        {
            if (!TryAddState(specification, evidence, scenarioSubject, state, SpecificationStepPhase.Then, stepIndex++, stepFacts, valueFacts))
            {
                return null;
            }
        }

        foreach (var (error, index) in specification.Errors.Select((error, index) => (error, index)))
        {
            var key = StepKey(scenarioSubject, stepIndex++);
            stepFacts.Add(new()
            {
                Id = FactId("step", scenarioSubject, key.Index.ToString(CultureInfo.InvariantCulture)),
                Subject = StepSubject(scenarioSubject, key.Index),
                Evidence = _sourceEvidence.For(evidence.Errors[index], "The assertion states an exact rejected outcome"),
                Definition = new SpecificationStepDefinition
                {
                    Key = key,
                    Phase = SpecificationStepPhase.Then,
                    Kind = SpecificationStepKind.Error,
                    ErrorMessage = string.IsNullOrEmpty(error) ? null : error
                }
            });
        }

        var scenario = new SpecificationScenarioFact
        {
            Id = FactId("scenario", scenarioSubject),
            Subject = scenarioSubject,
            Evidence = _sourceEvidence.For(evidence.Source, "The authored type is an exact Arc specification scenario"),
            Definition = new SpecificationScenarioDefinition
            {
                Key = new() { Scenario = scenarioSubject },
                Name = specification.Name,
                TargetArtifact = targetKey,
                Steps = [.. stepFacts.Select(step => step.Definition.Key)]
            }
        };
        _facts.Add(scenario);
        _facts.AddRange(stepFacts);
        _facts.AddRange(valueFacts);

        var placement = ArtifactFacts.PlacementRequest(targetKey, sliceKind, options.SourceStructurePolicy);
        if (placement is null)
        {
            if (sourceStructures.Diagnostics.Count == 0)
            {
                _sourceEvidence.Block(specification, evidence, "the target artifact has no exact shared source structure");
            }

            return null;
        }

        return new(placement, _facts);
    }

    bool TryAddState(
        SpecificationModel specification,
        SpecificationScenarioEvidence evidence,
        SubjectId scenario,
        SpecificationStateModel state,
        SpecificationStepPhase phase,
        int index,
        List<SpecificationStepFact> steps,
        List<SpecificationValueFact> values)
    {
        if (!evidence.States.TryGetValue(state, out var stateEvidence) || stateEvidence.Artifact is not INamedTypeSymbol artifact)
        {
            _sourceEvidence.Block(specification, evidence, $"step {index} has no exact source artifact evidence");
            return false;
        }

        var kind = Kind(state.Kind);
        var artifactKey = ArtifactFacts.Artifact(artifact, ArtifactKindFor(kind), stateEvidence.Source);
        if (kind == SpecificationStepKind.Unknown || artifactKey is null)
        {
            _sourceEvidence.Block(specification, evidence, $"step {index} has an unsupported or ambiguous artifact");
            return false;
        }

        if (kind != SpecificationStepKind.ReadModel && !HasEveryRequiredConstructionValue(artifact, state.Values.Count()))
        {
            _sourceEvidence.Block(specification, evidence, $"step {index} omits a required computed or unreadable construction value");
            return false;
        }

        var key = StepKey(scenario, index);
        var valueKeys = new List<SpecificationValueKey>();
        foreach (var value in state.Values)
        {
            if (!ArcSpecificationValueFacts.TryAdd(
                    context,
                    _sourceEvidence.For,
                    evidence.Values,
                    key,
                    artifact,
                    value,
                    values,
                    out var valueKey,
                    out var reason))
            {
                _sourceEvidence.Block(specification, evidence, reason!);
                return false;
            }

            valueKeys.Add(valueKey!);
        }

        steps.Add(new()
        {
            Id = FactId("step", scenario, index.ToString(CultureInfo.InvariantCulture)),
            Subject = StepSubject(scenario, index),
            Evidence = _sourceEvidence.For(stateEvidence.Source, "The exact allowlisted scenario API establishes this step"),
            Definition = new SpecificationStepDefinition
            {
                Key = key,
                Phase = phase,
                Kind = kind,
                Artifact = artifactKey,
                Values = valueKeys
            }
        });
        return true;
    }
}
