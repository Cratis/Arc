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
/// <param name="facts">The facts to contribute atomically.</param>
/// <param name="diagnostics">The diagnostics to append.</param>
internal sealed class ArcSpecificationFactBuilder(
    DotNetAnalysisContext context,
    DotNetProjectCompilation scenarioProject,
    AdapterIdentity adapter,
    DotNetAdapterOptions options,
    List<GenerationFact> facts,
    List<GenerationDiagnostic> diagnostics)
{
    readonly ArcSpecificationArtifactFacts _artifactFacts = new(context, scenarioProject, adapter, options, facts);
    readonly ArcSpecificationEvidence _sourceEvidence = new(context, scenarioProject, adapter, diagnostics);

    /// <summary>
    /// Adds one scenario only when every required step, value, artifact, and source location is exact.
    /// </summary>
    /// <param name="specification">The recovered legacy specification.</param>
    /// <param name="evidence">The exact source evidence.</param>
    /// <param name="target">The exact command or read-model target.</param>
    public void Add(
        SpecificationModel specification,
        SpecificationScenarioEvidence evidence,
        INamedTypeSymbol target)
    {
        if (evidence.Blockers.Count > 0 || SpecificationMembers.ReadModelOf(evidence.SourceType) is not null ||
            HasUnrepresentedEventPredicate(specification, evidence))
        {
            _sourceEvidence.Block(specification, evidence, "the existing Arc analyzer cannot prove every authored step and value exactly");
            return;
        }

        var scenarioSubject = scenarioProject.SubjectForType(evidence.SourceType);
        var targetKey = _artifactFacts.Artifact(target, ArtifactKind.Command, evidence.Source);
        if (targetKey is null)
        {
            _sourceEvidence.Block(specification, evidence, "the target artifact has no unique analyzed source identity");
            return;
        }

        var stepFacts = new List<SpecificationStepFact>();
        var valueFacts = new List<SpecificationValueFact>();
        var stepIndex = 0;
        foreach (var state in specification.Given)
        {
            if (!TryAddState(specification, evidence, scenarioSubject, state, SpecificationStepPhase.Given, stepIndex++, stepFacts, valueFacts))
            {
                return;
            }
        }

        if (specification.When is null ||
            !TryAddState(specification, evidence, scenarioSubject, specification.When, SpecificationStepPhase.When, stepIndex++, stepFacts, valueFacts))
        {
            return;
        }

        foreach (var state in specification.Then)
        {
            if (!TryAddState(specification, evidence, scenarioSubject, state, SpecificationStepPhase.Then, stepIndex++, stepFacts, valueFacts))
            {
                return;
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
        facts.Add(scenario);
        facts.AddRange(stepFacts);
        facts.AddRange(valueFacts);
        _artifactFacts.AddPlacement(targetKey, target, evidence.Source);
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
        var artifactKey = _artifactFacts.Artifact(artifact, ArtifactKindFor(kind), stateEvidence.Source);
        if (kind == SpecificationStepKind.Unknown || artifactKey is null)
        {
            _sourceEvidence.Block(specification, evidence, $"step {index} has an unsupported or ambiguous artifact");
            return false;
        }

        if (!HasEveryRequiredConstructionValue(artifact, state.Values.Count()))
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
