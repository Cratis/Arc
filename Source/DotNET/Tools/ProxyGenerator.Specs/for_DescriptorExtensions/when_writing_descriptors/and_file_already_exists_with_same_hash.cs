// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_file_already_exists_with_same_hash : Specification, IDisposable
{
    string _tempDir = null!;
    string _filePath = null!;
    TypeDescriptor _descriptor = null!;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles = null!;
    List<string> _directories = null!;
    List<Type> _typesInvolved = null!;
    List<string> _messages = null!;
    string _originalContent = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _generatedFiles = [];
        _directories = [];
        _typesInvolved = [];
        _messages = [];

        var testType = typeof(SimpleType);
        _descriptor = new TypeDescriptor(
            testType,
            "SimpleType",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        var path = testType.ResolveTargetPath(0);
        _filePath = Path.Join(_tempDir, path, "SimpleType.ts");
        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);

#pragma warning disable MA0136 // Raw String contains an implicit end of line character
        const string proxyContent = """
            /*---------------------------------------------------------------------------------------------
             *  **DO NOT EDIT** - This file is an automatically generated file.
             *--------------------------------------------------------------------------------------------*/

            /* eslint-disable sort-imports */
            // eslint-disable-next-line header/header
            import { field } from '@cratis/fundamentals';

            export class SimpleType {
            }

            """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character
        var hash = GeneratedFileMetadata.ComputeHash(proxyContent);
        var metadata = new GeneratedFileMetadata("Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors.SimpleType", DateTime.UtcNow, hash);
        _originalContent = $"{metadata.ToCommentLine()}{Environment.NewLine}{proxyContent}";
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
    }

    [Fact] void should_not_modify_file() => File.ReadAllText(_filePath).ShouldEqual(_originalContent);
    [Fact] void should_report_skipped_file() => _messages.ShouldContain(m => m.Contains("unchanged"));
    [Fact] void should_track_file_in_generated_files() => _generatedFiles.ContainsKey(_filePath).ShouldBeTrue();

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class SimpleType;