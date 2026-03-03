// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TargetFolderIndexer.when_building_type_to_output_file_map;

public class with_file_containing_multiple_types : Specification
{
    string _tempDir = null!;
    IReadOnlyDictionary<string, string> _result = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var metadata = new GeneratedFileMetadata("MyNamespace.TypeA, MyNamespace.TypeB", DateTime.UtcNow);
        File.WriteAllText(Path.Combine(_tempDir, "SharedFile.ts"), $"{metadata.ToCommentLine()}\nexport class TypeA {{}}\nexport class TypeB {{}}");
    }

    void Because() => _result = TargetFolderIndexer.BuildTypeToOutputFileMap(_tempDir);

    [Fact] void should_contain_first_type() => _result.ContainsKey("MyNamespace.TypeA").ShouldBeTrue();
    [Fact] void should_contain_second_type() => _result.ContainsKey("MyNamespace.TypeB").ShouldBeTrue();
    [Fact] void should_map_first_type_to_shared_file() => _result["MyNamespace.TypeA"].ShouldEqual("SharedFile");
    [Fact] void should_map_second_type_to_shared_file() => _result["MyNamespace.TypeB"].ShouldEqual("SharedFile");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
