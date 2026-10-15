// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Uses reference execution rather than a second rule evaluator to check concrete generated pins.
/// </summary>
internal static class GeneratedFixtureRules
{
    /// <summary>
    /// Checks a literal against the admitted concept's shape and rules.
    /// </summary>
    /// <param name="concept">The emitted validated concept.</param>
    /// <param name="value">The fixture text.</param>
    /// <returns>Whether the reference runner accepts the generated fixture.</returns>
    internal static bool Accepts(ConceptSyntax concept, string value)
    {
        var literal = JsonSerializer.Serialize(value);
        var source = GenerationAdmission.ProbeSource(concept) +
            $"      specification Fixture\n        when Allocate\n          generated value = {literal}\n        then returns {literal}\n";
        const string Key = "fixture";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GenerationProbe"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "fixture.play", source);
        var bound = new SemanticModelCompiler().Compile("GenerationProbe", SemanticDocumentSet.Create([document], catalog));
        if (!bound.Success || bound.Value is null || SemanticExecutionPlan.Compile(bound.Value.Model).Plan is not { } plan)
        {
            return false;
        }

        return new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id).Passed;
    }
}
