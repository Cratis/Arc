// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager;

public class when_preserving_an_authored_index_header : Specification
{
    string _directory = null!;
    Dictionary<string, GeneratedFileMetadata> _files = [];
    string _content = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "index.ts"), "// Authored header\nimport './setup';\nexport * from '@example/package';\n");
        var path = Path.Combine(_directory, "Sample.ts");
        File.WriteAllText(path, "export class Sample {}");
        _files[path] = new("Sample", DateTime.UtcNow, "ABC", true);
    }

    void Because()
    {
        IndexFileManager.UpdateIndexFileWithFileHeader("Application", _directory, _files, [], _ => { }, _directory);
        _content = File.ReadAllText(Path.Combine(_directory, "index.ts"));
    }

    [Fact] void should_preserve_authored_comments_and_imports() => _content.StartsWith($"// Authored header{Environment.NewLine}import './setup';", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_replace_the_authored_header() => _content.ShouldNotContain("// Application");
    [Fact] void should_preserve_the_package_export() => _content.ShouldContain("export * from '@example/package';");
    [Fact] void should_add_the_generated_export() => _content.ShouldContain("export * from './Sample';");

    void Destroy() => Directory.Delete(_directory, true);
}
