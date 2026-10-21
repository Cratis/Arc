// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_concept_fixture_conversions.given;

public class a_referenced_concept_fixture : Specification
{
    protected const string Concept = """
        using Cratis.Concepts;
        namespace Library.Approvals.Requesting;
        public record ApprovalLabel(string Value) : ConceptAs<string>(Value)
        {
            public static implicit operator ApprovalLabel(string value) => new(value);
        }
        """;

    protected ScreenplayGenerationResult Result;

    protected static Compilation Application(MetadataReference? reference, bool collection = false, bool framework = false)
    {
        var type = framework ? "Cratis.Chronicle.Events.EventSourceId" : "ApprovalLabel";
        var propertyType = collection ? $"System.Collections.Generic.IEnumerable<{type}>" : type;
        var value = collection ? "[\"urgent\", \"review\"]" : "\"urgent\"";
        var predicate = collection ? $"e.Label.SequenceEqual<{type}>({value})" : $"e.Label == {value}";
        var identifier = framework && !collection ? string.Empty : "[Key] string Id, ";
        var arguments = framework && !collection ? value : $"\"approval-1\", {value}";
        var eventSource = framework && !collection ? "urgent" : "approval-1";
        var source = $$"""
            using System.Linq;
            using System.Threading.Tasks;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Chronicle.Testing.EventSequences;
            using Xunit;
            namespace Library.Approvals.Requesting;
            [EventType] public record ApprovalRequested({{propertyType}} Label);
            [Command] public record RequestApproval({{identifier}}{{propertyType}} Label)
            {
                public ApprovalRequested Handle() => new(Label);
            }
            namespace when_requesting
            {
                public class and_a_label_is_supplied
                {
                    readonly CommandScenario<RequestApproval> _scenario = new();
                    Task Because() => _scenario.Execute(new RequestApproval({{arguments}}));
                    [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<ApprovalRequested>("{{eventSource}}", e => {{predicate}});
                }
            }
            """;

        // Block namespaces let this hermetic project hold both the slice and its scenario.
        source = source.Replace("namespace Library.Approvals.Requesting;", "namespace Library.Approvals.Requesting {", StringComparison.Ordinal) + "}";
        var application = Analyzed.Project(
            "Library",
            reference is null ? [] : [reference],
            (Analyzed.SlicePath, source),
            (IntegrationTesting.Path, IntegrationTesting.Source));
        Analyzed.ErrorsIn(application).ShouldBeEmpty();
        return application;
    }

    protected void AssertStated(bool collection = false)
    {
        Assert.True(Result.Source.Contains("specification WhenRequestingAndALabelIsSupplied", StringComparison.Ordinal), string.Join(Environment.NewLine, Result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        Result.Source.ShouldContain(collection ? "label = [\"urgent\",\"review\"]" : "label = \"urgent\"");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification || diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    }

    protected void AssertRefused()
    {
        Result.Source.ShouldNotContain("specification WhenRequestingAndALabelIsSupplied");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("conversion", StringComparison.Ordinal));
        Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    }
}
