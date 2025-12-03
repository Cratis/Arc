// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_getting_all_files;

public class with_empty_index : given.a_generated_file_index
{
    IEnumerable<string> _result;

    void Because() => _result = _index.GetAllFiles();

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
