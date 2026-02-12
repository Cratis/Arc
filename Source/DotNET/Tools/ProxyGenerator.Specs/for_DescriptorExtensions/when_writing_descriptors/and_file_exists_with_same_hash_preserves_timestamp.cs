// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_file_exists_with_same_hash_preserves_timestamp : Specification, IDisposable
{
    string _tempDir = null!;
    string _filePath = null!;
    TypeDescriptor _descriptor = null!;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles = null!;
    List<string> _directories = null!;
    List<Type> _typesInvolved = null!;
    List<string> _messages = null!;
    DateTime _originalTimestamp;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _generatedFiles = [];
        _directories = [];
        _typesInvolved = [];
        _messages = [];

        var testType = typeof(TimestampTestType);
        _descriptor = new TypeDescriptor(
            testType,
            "TimestampTestType",
            [],
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);

        var path = testType.ResolveTargetPath(0);
        _filePath = Path.Join(_tempDir, path, "TimestampTestType.ts");
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

            export class TimestampTestType {
            }

            """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character
        var hash = GeneratedFileMetadata.ComputeHash(proxyContent);
        _originalTimestamp = DateTime.UtcNow.AddDays(-1);
        var metadata = new GeneratedFileMetadata("Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors.TimestampTestType", _originalTimestamp, hash);
        var originalContent = $"{metadata.ToCommentLine()}{Environment.NewLine}{proxyContent}";
        File.WriteAllText(_filePath, originalContent);
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
            _ => { },
            _generatedFiles);
    }

    [Fact] void should_preserve_original_timestamp_in_generated_files_dictionary()
    {
        var normalizedPath = Path.GetFullPath(_filePath);
        var preservedTime = _generatedFiles[normalizedPath].GeneratedTime.ToUniversalTime();
        var originalTime = _originalTimestamp.ToUniversalTime();
        var timeDifference = Math.Abs((preservedTime - originalTime).TotalSeconds);
        timeDifference.ShouldBeLessThan(1.0);
    }

    [Fact] void should_preserve_original_timestamp_in_file_content()
    {
        var fileContent = File.ReadAllText(_filePath);
        var firstLine = fileContent.Split(Environment.NewLine)[0];
        firstLine.ShouldContain(_originalTimestamp.ToString("O"));
    }

    [Fact] void should_not_write_file_to_disk()
    {
        var normalizedPath = Path.GetFullPath(_filePath);
        _generatedFiles[normalizedPath].WasWritten.ShouldBeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class TimestampTestType;
