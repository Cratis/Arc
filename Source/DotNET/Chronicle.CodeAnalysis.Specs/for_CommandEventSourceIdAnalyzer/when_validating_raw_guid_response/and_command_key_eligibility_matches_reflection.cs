// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Cratis.Arc.Chronicle.Commands;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given.response_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_command_key_eligibility_matches_reflection
{
    [Theory]
    [InlineData("public record B { [Key] public Guid Id { get; init; } }", "", true)]
    [InlineData("public record B([Key] Guid Id) { public B() : this(Guid.NewGuid()) {} }", "", true)]
    [InlineData("public record B(EventSourceId<Guid> Id) { public B() : this(Guid.NewGuid()) {} }", "", true)]
    [InlineData("public record B { public EventSourceId Id { get; init; } = EventSourceId.New(); }", "", true)]
    [InlineData("public record Identity(Guid Value) : EventSourceId<Guid>(Value); public record B { public Identity Id { get; init; } = new(Guid.NewGuid()); }", "", true)]
    [InlineData("public record B { [Key] public virtual Guid Id { get; init; } }", "public override Guid Id { get; init; }", true)]
    [InlineData("public record B { [Key] public Guid Id { get; init; } }", "public new string Id { get; init; } = string.Empty;", true)]
    [InlineData("public record B { [Key] public Guid Id { get; init; } }", "private new Guid Id { get; init; }", false)]
    [InlineData("public record B;", "[Key] public static Guid Id { get; set; }", true)]
    [InlineData("public record Ancestor { [Key] public Guid Id { get; init; } } public record B : Ancestor { private new Guid Id { get; init; } }", "", true)]
    [InlineData("public record Base<T>([Key] T Id) { public Base() : this(default(T)!) {} } public record B : Base<Guid>;", "", true)]
    [InlineData("public record B;", "[Key] public Guid this[int index] => Guid.Empty;", true)]
    [InlineData("public record B;", "[Key] public Guid Id { private get; set; }", true)]
    [InlineData("public record B;", "public C([Key] Guid Id) { this.Id = Id; } public C() {} public Guid Id { get; init; }", true)]
    [InlineData("public record Convertible { public static implicit operator EventSourceId(Convertible value) => EventSourceId.New(); } public record B;", "public Convertible Id { get; init; } = new();", false)]
    [InlineData("public record Convertible { public static implicit operator EventSourceId(Convertible value) => EventSourceId.New(); } public record B;", "[Key] public Convertible Id { get; init; } = new();", true)]
    [InlineData("public record B { [Key] public Guid Id { get; init; } }", "public new Guid Id { get; init; }", false)]
    [InlineData("public record B { [Key] public static Guid Id { get; set; } }", "", false)]
    [InlineData("public record B { [Key] private Guid Id { get; init; } }", "", false)]
    [InlineData("public record B { public Guid Id; }", "", false)]
    [InlineData("public record B;", "public C([Key] Guid id) { Id = id; } public C() {} public Guid Id { get; init; }", false)]
    [InlineData("public record B;", "public C([Key] string Id) {} public C() {} public Guid Id { get; init; }", false)]
    [InlineData("public record B { public B([Key] Guid Id) {} public B() {} public virtual Guid Id { get; init; } }", "public override Guid Id { get; init; }", false)]
    public async Task should_match_runtime_property_discovery_without_changing_the_ambiguity_rule(string baseType, string members, bool hasRuntimeKey)
    {
        var source = Preamble + baseType + "\n[Command] public record C : B { " + members + " public {|#0:(Guid, E)|} Handle() => (Guid.NewGuid(), new E()); }";

        // Assert against the real reflection implementation as well as the analyzer, including hiding and overrides.
        var compilation = await TestProject.CreateProject(SourceMarker.Parse(source).Source).GetCompilationAsync();
        await using var stream = new MemoryStream();
        var emitted = compilation!.Emit(stream);
        if (!emitted.Success)
        {
            throw new SnippetDoesNotCompile(emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }

        var command = Activator.CreateInstance(Assembly.Load(stream.ToArray()).GetType("C")!);
        command!.HasEventSourceId().ShouldEqual(hasRuntimeKey);
        await VerifyCS.VerifyAnalyzerAsync(source, hasRuntimeKey ? [] : [Warning()]);
    }
}
