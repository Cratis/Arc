// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_file_does_not_exist : Specification, IDisposable
{
    string _tempDir = null!;
    string _filePath = null!;
    TypeDescriptor _descriptor = null!;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles = null!;
    List<string> _directories = null!;
    List<Type> _typesInvolved = null!;
    List<string> _messages = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _generatedFiles = [];
        _directories = [];
        _typesInvolved = [];
        _messages = [];

        var testType = typeof(NewType);
        _descriptor = new TypeDescriptor(
            testType,
            "NewType",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        var path = testType.ResolveTargetPath(0);
        _filePath = Path.Join(_tempDir, path, "NewType.ts");
    }

    async Task Because()
    {
        await new[] { _descriptor }.Write(
            _tempDir,
            _typesInvolved,
            TemplateTypes.Type,
            _directories,
            0,
            "types",
            _messages.Add,
            _generatedFiles);
    }

    [Fact] void should_create_file() => File.Exists(_filePath).ShouldBeTrue();
    [Fact] void should_not_report_skipped_file() => _messages.Where(m => m.Contains("unchanged")).ShouldBeEmpty();
    [Fact] void should_track_file_in_generated_files() => _generatedFiles.ContainsKey(_filePath).ShouldBeTrue();
    [Fact] void should_contain_hash() => File.ReadAllText(_filePath).ShouldContain("Hash:");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class NewType
{
}
