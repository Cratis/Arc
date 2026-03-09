// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_SourceFileResolver;

public class when_resolving_unmapped_types_with_ambiguous_namespace : Specification
{
    Dictionary<string, string> _result = null!;

    void Establish()
    {
        _result = new Dictionary<string, string>
        {
            ["MyApp.Models.Customer"] = "Customer",
            ["MyApp.Models.Order"] = "Order"
        };
    }

    void Because() => SourceFileResolver.ResolveUnmappedTypes(
        _result,
        [("MyApp.Models.Status", "MyApp.Models")]);

    [Fact] void should_not_resolve_when_multiple_source_files_in_namespace() =>
        _result.ContainsKey("MyApp.Models.Status").ShouldBeFalse();
}
