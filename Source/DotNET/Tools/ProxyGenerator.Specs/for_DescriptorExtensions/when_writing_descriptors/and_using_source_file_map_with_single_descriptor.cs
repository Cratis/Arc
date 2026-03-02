// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_using_source_file_map_with_single_descriptor : Specification, IDisposable
{
    string _tempDir = null!;
    string _expectedFilePath = null!;
    TypeDescriptor _descriptor = null!;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles = null!;
    List<string> _directories = null!;
    List<Type> _typesInvolved = null!;
    Dictionary<string, string> _sourceFileMap = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _generatedFiles = [];
        _directories = [];
        _typesInvolved = [];

        var testType = typeof(SingleSourceType);

        _descriptor = new TypeDescriptor(
            testType,
            "SingleSourceType",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        _sourceFileMap = new Dictionary<string, string>
        {
            [testType.FullName!] = "MySourceFile"
        };

        var path = testType.ResolveTargetPath(0);
        _expectedFilePath = Path.GetFullPath(Path.Join(_tempDir, path, "MySourceFile.ts"));
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
            _ => { },
            _ => { },
            _generatedFiles,
            sourceFileMap: _sourceFileMap);
    }

    [Fact] void should_create_file_named_after_source_file() => File.Exists(_expectedFilePath).ShouldBeTrue();
    [Fact] void should_contain_type() => File.ReadAllText(_expectedFilePath).ShouldContain("SingleSourceType");
    [Fact] void should_track_file_in_generated_files() => _generatedFiles.ContainsKey(_expectedFilePath).ShouldBeTrue();

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class SingleSourceType;
