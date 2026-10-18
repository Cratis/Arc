// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_DescriptorExtensions;

public class when_writing_all_templates_with_a_header : Specification
{
    string _directory = null!;
    string[] _paths = [];
    Dictionary<string, (string Content, string SourceTypeName)> _pending = [];
    Dictionary<string, GeneratedFileMetadata> _files = [];
    string[] _firstOutput = [];

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        var templates = new[] { TemplateTypes.Command, TemplateTypes.Query, TemplateTypes.ObservableQuery, TemplateTypes.Type, TemplateTypes.Interface, TemplateTypes.Enum, TemplateTypes.FlagsEnum };
        for (var i = 0; i < templates.Length; i++)
        {
            var path = Path.Combine(_directory, $"Sample{i}.ts");
            _pending[path] = (templates[i](new { Name = $"Sample{i}" }), $"Sample{i}");
        }
        _paths = [.. _pending.Keys];
    }

    async Task Because()
    {
        await DescriptorExtensions.FlushPendingContentWithFileHeader("Application\\n{fileName}", _pending, _files, _ => { });
        _firstOutput = _paths.Select(File.ReadAllText).ToArray();
        await DescriptorExtensions.FlushPendingContentWithFileHeader("Application\\n{fileName}", _pending, _files, _ => { });
    }

    [Fact] void should_start_every_file_with_the_application_header() => _paths.All(path => File.ReadAllText(path).StartsWith($"// Application\n// {Path.GetFileName(path)}\n\n", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_emit_a_cratis_copyright() => _paths.All(path => !File.ReadAllText(path).Contains("Copyright (c) Cratis", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_every_file_recognizable_as_generated() => _paths.All(path => GeneratedFileMetadata.IsGeneratedFile(path, out _)).ShouldBeTrue();
    [Fact] void should_not_rewrite_identical_files() => _files.Values.All(metadata => !metadata.WasWritten).ShouldBeTrue();
    [Fact] void should_preserve_identical_output() => _paths.Select(File.ReadAllText).ShouldEqual(_firstOutput);

    void Destroy() => Directory.Delete(_directory, true);
}
