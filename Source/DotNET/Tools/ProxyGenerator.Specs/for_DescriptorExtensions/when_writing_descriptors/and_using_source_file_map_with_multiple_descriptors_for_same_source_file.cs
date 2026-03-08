// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_using_source_file_map_with_multiple_descriptors_for_same_source_file : Specification, IDisposable
{
    string _tempDir = null!;
    string _expectedFilePath = null!;
    TypeDescriptor _firstDescriptor = null!;
    TypeDescriptor _secondDescriptor = null!;
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

        var firstType = typeof(SourceFileType1);
        var secondType = typeof(SourceFileType2);

        _firstDescriptor = new TypeDescriptor(
            firstType,
            "SourceFileType1",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        _secondDescriptor = new TypeDescriptor(
            secondType,
            "SourceFileType2",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        _sourceFileMap = new Dictionary<string, string>
        {
            [firstType.FullName!] = "SharedSourceFile",
            [secondType.FullName!] = "SharedSourceFile"
        };

        var path = firstType.ResolveTargetPath(0);
        _expectedFilePath = Path.GetFullPath(Path.Join(_tempDir, path, "SharedSourceFile.ts"));
    }

    async Task Because()
    {
        await new[] { _firstDescriptor, _secondDescriptor }.Write(
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

    [Fact] void should_create_combined_file() => File.Exists(_expectedFilePath).ShouldBeTrue();
    [Fact] void should_not_create_individual_files() => File.Exists(Path.Join(_tempDir, "SourceFileType1.ts")).ShouldBeFalse();
    [Fact] void should_contain_first_type() => File.ReadAllText(_expectedFilePath).ShouldContain("SourceFileType1");
    [Fact] void should_contain_second_type() => File.ReadAllText(_expectedFilePath).ShouldContain("SourceFileType2");
    [Fact] void should_track_combined_file_in_generated_files() => _generatedFiles.ContainsKey(_expectedFilePath).ShouldBeTrue();

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class SourceFileType1;
public class SourceFileType2;
