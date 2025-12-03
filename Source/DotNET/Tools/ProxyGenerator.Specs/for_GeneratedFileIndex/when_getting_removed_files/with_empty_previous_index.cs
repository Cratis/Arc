// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_getting_removed_files;

public class with_empty_previous_index : Specification
{
    GeneratedFileIndex _previousIndex;
    GeneratedFileIndex _currentIndex;
    IEnumerable<string> _result;

    void Establish()
    {
        _previousIndex = new GeneratedFileIndex();

        _currentIndex = new GeneratedFileIndex();
        _currentIndex.AddFile("folder1/file1.ts");
    }

    void Because() => _result = _currentIndex.GetRemovedFiles(_previousIndex);

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
