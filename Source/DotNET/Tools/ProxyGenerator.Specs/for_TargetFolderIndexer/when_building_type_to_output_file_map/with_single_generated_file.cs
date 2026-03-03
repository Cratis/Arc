// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TargetFolderIndexer.when_building_type_to_output_file_map;

public class with_single_generated_file : Specification
{
    string _tempDir = null!;
    IReadOnlyDictionary<string, string> _result = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var metadata = new GeneratedFileMetadata("MyNamespace.MyType", DateTime.UtcNow);
        File.WriteAllText(Path.Combine(_tempDir, "MySourceFile.ts"), $"{metadata.ToCommentLine()}\nexport class MyType {{}}");
    }

    void Because() => _result = TargetFolderIndexer.BuildTypeToOutputFileMap(_tempDir);

    [Fact] void should_contain_the_type() => _result.ContainsKey("MyNamespace.MyType").ShouldBeTrue();
    [Fact] void should_map_type_to_correct_file_name() => _result["MyNamespace.MyType"].ShouldEqual("MySourceFile");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
