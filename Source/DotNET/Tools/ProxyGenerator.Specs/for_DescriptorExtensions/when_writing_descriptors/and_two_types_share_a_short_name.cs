// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

/// <summary>
/// Two different types share the short name `Shared`. One lives in a file named after itself, the other in
/// a file named something else. The one that moves must not be allowed to rewrite imports of the one that
/// does not - a short name claimed by two types is ambiguous, so neither gets a fixup.
/// </summary>
public class and_two_types_share_a_short_name : Specification, IDisposable
{
    string _tempDir = null!;
    string _expectedFilePath = null!;
    TypeDescriptor _descriptor = null!;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles = null!;
    List<string> _directories = null!;
    List<Type> _typesInvolved = null!;
    Dictionary<string, string> _sourceFileMap = null!;
    string _fileContent = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _generatedFiles = [];
        _directories = [];
        _typesInvolved = [];

        var descriptorType = typeof(ImporterOfShared);
        var importedType = typeof(Shared);

        var imports = new List<ImportStatement> { new(importedType, "Shared", "./Shared") };

        _descriptor = new TypeDescriptor(
            descriptorType,
            "ImporterOfShared",
            [new PropertyDescriptor(importedType, "myProp", "Shared", string.Empty, string.Empty, false, false, false, null)],
            imports.OrderBy(_ => _.Module),
            [importedType]);

        _sourceFileMap = new Dictionary<string, string>
        {
            [descriptorType.FullName!] = "ImporterOfShared",

            // This one's file is named after it, so it needs no rewrite - but it does claim the name.
            [importedType.FullName!] = "Shared",

            // A different type, same short name, in a file called something else entirely.
            ["Some.Other.Namespace.Shared"] = "Grouped"
        };

        var path = descriptorType.ResolveTargetPath(0);
        _expectedFilePath = Path.GetFullPath(Path.Join(_tempDir, path, "ImporterOfShared.ts"));
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

        _fileContent = await File.ReadAllTextAsync(_expectedFilePath);
    }

    [Fact] void should_leave_the_import_pointing_at_its_own_file() => _fileContent.ShouldContain("from './Shared'");
    [Fact] void should_not_redirect_it_to_the_other_types_file() => _fileContent.ShouldNotContain("from './Grouped'");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class ImporterOfShared;
public class Shared;
