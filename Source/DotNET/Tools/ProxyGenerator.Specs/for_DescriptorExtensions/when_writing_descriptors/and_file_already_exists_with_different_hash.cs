// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_file_already_exists_with_different_hash : Specification, IDisposable
{
    string _tempDir = null!;
    string _filePath = null!;
    TypeDescriptor _descriptor = null!;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles = null!;
    List<string> _directories = null!;
    List<Type> _typesInvolved = null!;
    List<string> _messages = null!;
    string _originalContent = null!;
    string _newContent = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _generatedFiles = [];
        _directories = [];
        _typesInvolved = [];
        _messages = [];

        var testType = typeof(ModifiedType);
        _descriptor = new TypeDescriptor(
            testType,
            "ModifiedType",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        var path = testType.ResolveTargetPath(0);
        _filePath = Path.Join(_tempDir, path, "ModifiedType.ts");
        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);

        const string oldProxyContent = "export class ModifiedType { oldProperty: string; }";
        var oldHash = GeneratedFileMetadata.ComputeHash(oldProxyContent);
        var oldMetadata = new GeneratedFileMetadata("Cratis.Arc.ProxyGenerator.ModifiedType", DateTime.UtcNow, oldHash);
        _originalContent = $"{oldMetadata.ToCommentLine()}{Environment.NewLine}{oldProxyContent}";
        File.WriteAllText(_filePath, _originalContent);
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

        _newContent = await File.ReadAllTextAsync(_filePath);
    }

    [Fact] void should_modify_file() => _newContent.ShouldNotEqual(_originalContent);
    [Fact] void should_not_report_skipped_file() => _messages.Where(m => m.Contains("unchanged")).ShouldBeEmpty();
    [Fact] void should_track_file_in_generated_files() => _generatedFiles.ContainsKey(_filePath).ShouldBeTrue();
    [Fact] void should_contain_new_hash() => _newContent.ShouldContain("Hash:");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class ModifiedType;