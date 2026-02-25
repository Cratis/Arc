// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_SourceFileResolver.when_building_type_to_source_file_map;

public class with_current_assembly : Specification
{
    IReadOnlyDictionary<string, string> _result = null!;

    void Because() => _result = SourceFileResolver.BuildTypeToSourceFileMap(typeof(SourceFileResolver).Assembly.Location);

    [Fact] void should_return_non_null_result() => _result.ShouldNotBeNull();
}
