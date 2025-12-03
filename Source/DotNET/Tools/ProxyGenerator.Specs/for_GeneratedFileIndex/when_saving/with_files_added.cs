// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_saving;

public class with_files_added : Specification
{
    GeneratedFileIndex _index;
    string _projectDirectory;
    string _indexFilePath;

    void Establish()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _indexFilePath = Path.Combine(_projectDirectory, ".cratis", "GeneratedFileIndex.json");
        _index = new GeneratedFileIndex();
        _index.AddFile("folder/file.ts");
    }

    void Because() => _index.Save(_projectDirectory);

    void Cleanup()
    {
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, true);
        }
    }

    [Fact] void should_create_cratis_folder() => Directory.Exists(Path.Combine(_projectDirectory, ".cratis")).ShouldBeTrue();
    [Fact] void should_create_index_file() => File.Exists(_indexFilePath).ShouldBeTrue();
}
