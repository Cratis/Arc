// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_getting_all_files;

public class with_multiple_files : given.a_generated_file_index
{
    IEnumerable<string> _result;

    void Establish()
    {
        _index.AddFile("folder1/file1.ts");
        _index.AddFile("folder1/file2.ts");
        _index.AddFile("folder2/subfolder/file3.ts");
    }

    void Because() => _result = _index.GetAllFiles();

    [Fact] void should_contain_file1() => _result.ShouldContain("folder1/file1.ts");
    [Fact] void should_contain_file2() => _result.ShouldContain("folder1/file2.ts");
    [Fact] void should_contain_file3() => _result.ShouldContain("folder2/subfolder/file3.ts");
    [Fact] void should_contain_exactly_three_files() => _result.Count().ShouldEqual(3);
}
