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
            [property: Required, Display(Name = "Customer name")] Text? Name,
            [property: StringLength(3)] string Label,
            Text Unannotated,
            string Plain)
        {
            public void Handle() { }
        }
        [Command] public record Unannotated(Text Name) { public void Handle() { } }
        [ReadModel] public record Report(string Name)
        {
            public static Report Search([Range(1, 5)] Number count, [Required] Text name) => new(name.Value);
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
