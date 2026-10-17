// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_query_parameter_types_are_diagnostic_sensitive : given.annotated_models
{
    readonly List<string> _registered = [];
    readonly List<string> _diagnosticMessages = [];

    void Because()
    {
        foreach (var attribute in new[] { "System.Obsolete(\"x\")", "System.Diagnostics.CodeAnalysis.Experimental(\"ANNOTATION001\")" })
        {
            var source = $$"""
                #pragma warning disable CS0618, ANNOTATION001
                using System.ComponentModel.DataAnnotations;
                using Cratis.Arc.Queries.ModelBound;
                using Cratis.Concepts;
                namespace Annotated;
                [{{attribute}}] public record Text(string Value) : ConceptAs<string>(Value);
                [ReadModel] public record Report(string Name)
                {
                    public static Report Search([Required] Text name) => new("value");
                }
                """;
            Compile(source);
            if (DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName), out _))
            {
                _registered.Add(attribute);
            }

            _diagnosticMessages.AddRange(_diagnostics.Select(diagnostic => diagnostic.ToString()));
        }
    }

    [Fact] void should_leave_the_queries_unregistered() => _registered.ShouldBeEmpty();
    [Fact] void should_compile_without_generated_diagnostics() => _diagnosticMessages.ShouldBeEmpty();
}
