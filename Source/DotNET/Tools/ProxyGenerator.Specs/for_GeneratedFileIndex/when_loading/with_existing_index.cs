// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_loading;

public class with_existing_index : Specification
{
    GeneratedFileIndex _originalIndex;
    GeneratedFileIndex _result;
    string _projectDirectory;

    void Establish()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _originalIndex = new GeneratedFileIndex();
        _originalIndex.AddFile("folder/file1.ts");
        _originalIndex.AddFile("folder/file2.ts");
        _originalIndex.Save(_projectDirectory);
    }

    void Because() => _result = GeneratedFileIndex.Load(_projectDirectory);

    void Cleanup()
    {
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, true);
        }
    }

    [Fact] void should_contain_original_files() => _result.GetAllFiles().Count().ShouldEqual(2);
    [Fact] void should_contain_file1() => _result.GetAllFiles().ShouldContain("folder/file1.ts");
    [Fact] void should_contain_file2() => _result.GetAllFiles().ShouldContain("folder/file2.ts");
}
