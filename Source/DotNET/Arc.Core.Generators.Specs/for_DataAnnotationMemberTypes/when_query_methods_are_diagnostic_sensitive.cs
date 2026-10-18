// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_query_methods_are_diagnostic_sensitive : given.annotated_models
{
    readonly List<string> _registered = [];
    readonly List<string> _diagnosticMessages = [];

    void Because()
    {
        string[] attributes = [
            "System.Obsolete(\"x\", true)",
            "System.Diagnostics.CodeAnalysis.Experimental(\"ANNOTATION001\")",
            "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"x\")",
            "System.Diagnostics.CodeAnalysis.RequiresDynamicCode(\"x\")"
        ];
        foreach (var attribute in attributes)
        {
            Compile(Models.Replace("public static Report Search(", $"[{attribute}] public static Report Search(", StringComparison.Ordinal));
            if (DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName), out _))
            {
                _registered.Add(attribute);
            }

            _diagnosticMessages.AddRange(_diagnostics.Select(diagnostic => diagnostic.ToString()));
        }
    }

    [Fact] void should_leave_all_flagged_queries_unregistered() => _registered.ShouldBeEmpty();
    [Fact] void should_compile_without_generated_diagnostics() => _diagnosticMessages.ShouldBeEmpty();
}
