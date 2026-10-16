// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_two_types_share_a_short_name : Specification
{
    string _outputPath;
    Dictionary<string, string> _sourceFileMap;
    string _firstContent;
    string _secondContent;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _sourceFileMap = new Dictionary<string, string>
        {
            [typeof(given.A.FeatureNode).FullName!] = "FeatureNode",
            [typeof(given.B.FeatureNode).FullName!] = "Grouped"
        };
        TypeExtensions.SetSourceFileMap(_sourceFileMap);
    }

    async Task Because()
    {
        Type[] types = [typeof(given.A.ModuleNode), typeof(given.A.FeatureNode), typeof(given.B.ModuleNode), typeof(given.B.FeatureNode)];
        var descriptors = types.Select(type => type.ToTypeDescriptor(_outputPath, 0)).ToArray();
        await descriptors.Write(_outputPath, [], TemplateTypes.Type, [], 0, "types", _ => { }, _ => { }, sourceFileMap: _sourceFileMap);
        _firstContent = await File.ReadAllTextAsync(Path.Join(_outputPath, typeof(given.A.ModuleNode).ResolveTargetPath(0), "ModuleNode.ts"));
        _secondContent = await File.ReadAllTextAsync(Path.Join(_outputPath, typeof(given.B.ModuleNode).ResolveTargetPath(0), "ModuleNode.ts"));
    }

    [Fact] void should_leave_the_first_import_pointing_at_its_own_file() => _firstContent.ShouldContain("from './FeatureNode'");
    [Fact] void should_resolve_the_second_import_to_its_source_file() => _secondContent.ShouldContain("from './Grouped'");
    [Fact] void should_not_redirect_the_first_import_to_the_other_types_file() => _firstContent.ShouldNotContain("from './Grouped'");
    [Fact] void should_write_the_first_imported_module() => File.Exists(Path.Join(_outputPath, typeof(given.A.FeatureNode).ResolveTargetPath(0), "FeatureNode.ts")).ShouldBeTrue();
    [Fact] void should_write_the_second_imported_module() => File.Exists(Path.Join(_outputPath, typeof(given.B.FeatureNode).ResolveTargetPath(0), "Grouped.ts")).ShouldBeTrue();

    void Destroy()
    {
        TypeExtensions.SetSourceFileMap(null);
        Directory.Delete(_outputPath, true);
    }
}
