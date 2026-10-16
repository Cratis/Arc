// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_an_unmapped_type_shares_a_mapped_short_name : Specification
{
    string _outputPath;
    Dictionary<string, string> _sourceFileMap;
    string _content;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _sourceFileMap = new Dictionary<string, string>
        {
            [typeof(given.B.FeatureNode).FullName!] = "StudioTree"
        };
        TypeExtensions.SetSourceFileMap(_sourceFileMap);
    }

    async Task Because()
    {
        var type = typeof(given.A.ModuleNode);
        var descriptor = type.ToTypeDescriptor(_outputPath, 0);
        await new[] { descriptor }.Write(_outputPath, [], TemplateTypes.Type, [], 0, "types", _ => { }, _ => { }, sourceFileMap: _sourceFileMap);
        _content = await File.ReadAllTextAsync(Path.Join(_outputPath, type.ResolveTargetPath(0), "ModuleNode.ts"));
    }

    [Fact] void should_import_the_unmapped_types_own_module() => _content.ShouldContain("from './FeatureNode'");
    [Fact] void should_not_import_the_unrelated_types_source_file() => _content.ShouldNotContain("from './StudioTree'");

    void Destroy()
    {
        TypeExtensions.SetSourceFileMap(null);
        Directory.Delete(_outputPath, true);
    }
}
