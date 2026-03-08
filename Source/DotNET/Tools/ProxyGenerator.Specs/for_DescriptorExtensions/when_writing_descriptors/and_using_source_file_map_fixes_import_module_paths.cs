// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_using_source_file_map_fixes_import_module_paths : Specification, IDisposable
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

        var descriptorType = typeof(ImportingType);
        var importedType = typeof(ImportedTypeWithDifferentSourceFile);

        // The import module path uses the type name (as GetImportStatement would generate).
        // With sourceFileMap, the imported type lives in a file named "SharedSlice" not "ImportedTypeWithDifferentSourceFile".
        var imports = new List<ImportStatement>
        {
            new(importedType, "ImportedTypeWithDifferentSourceFile", $"./{importedType.Name}")
        };

        _descriptor = new TypeDescriptor(
            descriptorType,
            "ImportingType",
            [new PropertyDescriptor(importedType, "myProp", "ImportedTypeWithDifferentSourceFile", string.Empty, string.Empty, false, false, false, null)],
            imports.OrderBy(_ => _.Module),
            [importedType]);

        _sourceFileMap = new Dictionary<string, string>
        {
            [descriptorType.FullName!] = "ImportingType",
            [importedType.FullName!] = "SharedSlice"
        };

        var path = descriptorType.ResolveTargetPath(0);
        _expectedFilePath = Path.GetFullPath(Path.Join(_tempDir, path, "ImportingType.ts"));
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

    [Fact] void should_create_output_file() => File.Exists(_expectedFilePath).ShouldBeTrue();
    [Fact] void should_fix_import_path_to_use_source_file_name() => _fileContent.ShouldContain("from './SharedSlice'");
    [Fact] void should_not_contain_original_type_name_in_import_path() => _fileContent.ShouldNotContain("from './ImportedTypeWithDifferentSourceFile'");
    [Fact] void should_keep_imported_type_name_in_braces() => _fileContent.ShouldContain("ImportedTypeWithDifferentSourceFile");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

public class ImportingType;
public class ImportedTypeWithDifferentSourceFile;
