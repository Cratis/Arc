// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_getting_removed_files;

public class with_files_removed_from_previous : Specification
{
    GeneratedFileIndex _previousIndex;
    GeneratedFileIndex _currentIndex;
    IEnumerable<string> _result;

    void Establish()
    {
        _previousIndex = new GeneratedFileIndex();
        _previousIndex.AddFile("folder1/file1.ts");
        _previousIndex.AddFile("folder1/file2.ts");
        _previousIndex.AddFile("folder2/file3.ts");

        _currentIndex = new GeneratedFileIndex();
        _currentIndex.AddFile("folder1/file1.ts");
    }

    void Because() => _result = _currentIndex.GetRemovedFiles(_previousIndex);

    [Fact] void should_contain_removed_file2() => _result.ShouldContain("folder1/file2.ts");
    [Fact] void should_contain_removed_file3() => _result.ShouldContain("folder2/file3.ts");
    [Fact] void should_not_contain_existing_file1() => _result.ShouldNotContain("folder1/file1.ts");
    [Fact] void should_contain_exactly_two_removed_files() => _result.Count().ShouldEqual(2);
}
