// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes.given;

public class annotated_models : Specification
{
    protected const string Models = """
        using System.ComponentModel.DataAnnotations;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Concepts;
        namespace Annotated;
        public record Text(string Value) : ConceptAs<string>(Value);
        public record Number(int Value) : ConceptAs<int>(Value);
        [Command] public partial record Submit(
            [property: Required, Display(Name = nameof(Labels.Customer), ResourceType = typeof(Labels))] Text? Name,
            [property: StringLength(3)] string Label,
            Text Unannotated,
            string Plain)
        {
            public void Handle() { }
        }
        public static class Labels { public static string Customer => "Customer name"; }
        [Command] public record Unannotated(Text Name) { public void Handle() { } }
        [Command] public record Ordinary([property: StringLength(3)] string Label) { public void Handle() { } }
        public record InheritedProperties { [StringLength(3)] public string Label { get; init; } = "long"; }
        [Command, Rejected] public record Derived([property: Required] Text Name) : InheritedProperties { public void Handle() { } }
        public sealed class RejectedAttribute : ValidationAttribute { public override bool IsValid(object? value) => false; }
        [ReadModel] public record Report(string Name)
        {
            public static Report Search([Range(1, 5)] Number count, [Required] Text name) => new(name.Value);
            internal static Report Internal([Range(1, 5)] Number count) => new("internal");
            public static Report Ordinary([Range(1, 100)] int count) => new("ordinary");
            public static Report Named([Required, StringLength(3), Display(Name = "Customer name")] string? name) => new(name!);
            public static Report Plain(int count) => new("plain");
            internal static Report InternalOrdinary([Range(1, 100)] int count) => new("internal");
        }
        """;

    protected string _source;
    protected Diagnostic[] _diagnostics;
    protected Assembly _assembly;
    protected string _queryName;

    protected void Compile(string source)
    {
        var readModel = $"Report_{Guid.NewGuid():N}";
        _queryName = $"Annotated.{readModel}.Search";
        var models = source.Replace("Report", readModel, StringComparison.Ordinal);
        var result = ModelGraphWalkerCompilation.Compile($"Annotations_{Guid.NewGuid():N}", models, true, [new QueryMetadataGenerator()]);
        _source = result.Source;
        _diagnostics = result.Diagnostics;
        _assembly = ModelGraphWalkerCompilation.Load(result.Output);
    }
}
