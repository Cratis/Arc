// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TargetFolderIndexer.when_building_type_to_output_file_map;

public class with_non_generated_file : Specification
{
    string _tempDir = null!;
    IReadOnlyDictionary<string, string> _result = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "ManualFile.ts"), "export class ManualType {}");
    }

    void Because() => _result = TargetFolderIndexer.BuildTypeToOutputFileMap(_tempDir);

    [Fact] void should_return_empty_map() => _result.ShouldBeEmpty();

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
