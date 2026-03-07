// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TargetFolderIndexer.when_building_type_to_output_file_map;

public class with_non_existent_output_folder : Specification
{
    IReadOnlyDictionary<string, string> _result = null!;

    void Because() => _result = TargetFolderIndexer.BuildTypeToOutputFileMap("/non/existent/output/path");

    [Fact] void should_return_empty_map() => _result.ShouldBeEmpty();
}
