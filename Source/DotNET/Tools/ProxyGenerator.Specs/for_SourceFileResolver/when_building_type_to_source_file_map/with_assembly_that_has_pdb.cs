// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_SourceFileResolver.when_building_type_to_source_file_map;

public class with_assembly_that_has_pdb : Specification
{
    IReadOnlyDictionary<string, string> _result = null!;

    void Because() => _result = SourceFileResolver.BuildTypeToSourceFileMap(typeof(SourceFileResolver).Assembly.Location);

    [Fact] void should_resolve_source_file_for_source_file_resolver() => _result.ContainsKey(typeof(SourceFileResolver).FullName!).ShouldBeTrue();
    [Fact] void should_map_source_file_resolver_to_its_file_name() => _result[typeof(SourceFileResolver).FullName!].ShouldEqual("SourceFileResolver");
}
