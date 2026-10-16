// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions.when_writing_descriptors;

public class and_siblings_share_a_generated_module : Specification
{
    string _outputPath;
    Dictionary<string, string> _sourceFileMap;
    string _content;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _sourceFileMap = new Dictionary<string, string>
        {
            [typeof(given.A.FeatureNode).FullName!] = "Same",
            [typeof(given.A.ModuleNode).FullName!] = "Same"
        };
        TypeExtensions.SetSourceFileMap(_sourceFileMap);
    }

    async Task Because()
    {
        var module = typeof(given.A.ModuleNode).ToTypeDescriptor(_outputPath, 0);
        var feature = typeof(given.A.FeatureNode).ToTypeDescriptor(_outputPath, 0);
        await new[] { module, feature }.Write(_outputPath, [], TemplateTypes.Type, [], 0, "types", _ => { }, _ => { }, sourceFileMap: _sourceFileMap);
        _content = await File.ReadAllTextAsync(Path.Join(_outputPath, module.Type.ResolveTargetPath(0), "Same.ts"));
    }

    [Fact] void should_not_emit_a_self_import() => _content.ShouldNotContain("from './Same'");
    [Fact] void should_keep_the_referenced_sibling() => _content.ShouldContain("export class FeatureNode");
    [Fact] void should_keep_the_importing_sibling() => _content.ShouldContain("export class ModuleNode");
    [Fact] void should_keep_the_sibling_property_type() => _content.ShouldContain("feature!: FeatureNode");

    void Destroy()
    {
        TypeExtensions.SetSourceFileMap(null);
        Directory.Delete(_outputPath, true);
    }
}
