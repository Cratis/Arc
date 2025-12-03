// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileIndex.when_loading;

public class with_no_existing_index : Specification
{
    GeneratedFileIndex _result;
    string _projectDirectory;

    void Establish() => _projectDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    void Because() => _result = GeneratedFileIndex.Load(_projectDirectory);

    void Cleanup()
    {
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, true);
        }
    }

    [Fact] void should_return_empty_index() => _result.Entries.ShouldBeEmpty();
}
