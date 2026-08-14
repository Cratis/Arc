// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

namespace Cratis.Arc.ProxyGenerator.for_SourceFileResolver.when_building_type_to_source_file_map;

public class with_a_type_lacking_debug_info : Specification
{
    IReadOnlyDictionary<string, string> _result = null!;

    void Because() => _result = SourceFileResolver.BuildTypeToSourceFileMap(typeof(Status).Assembly.Location);

    [Fact] void should_map_the_sibling_type_with_debug_info() =>
        _result[typeof(Status).FullName!].ShouldEqual("Status");

    [Fact] void should_not_guess_a_source_file_for_the_enum() =>
        _result.ContainsKey(typeof(StatusKind).FullName!).ShouldBeFalse();
}
