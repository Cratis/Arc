// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Cratis.Arc.Screenplay.Embedded.Hosting.Assets;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_reading_viewer_assets : Specification
{
    const string Script = "console.log('viewer');";

    EmbeddedViewerAssets _assets;
    bool _readTheScript;
    byte[] _script;
    string _scriptContentType;
    bool _readTheIndex;
    bool _readSomethingOutsideTheViewer;
    bool _readATraversal;
    bool _readAnAbsolutePath;

    void Establish() => _assets = EmbeddedViewerAssets.For(AnAssemblyWithAViewer());

    void Because()
    {
        _readTheScript = _assets.TryRead("assets/index.js", out _script, out _scriptContentType);
        _readTheIndex = _assets.TryRead(EmbeddedViewerAssets.IndexPath, out _, out _);
        _readSomethingOutsideTheViewer = _assets.TryRead("secrets.json", out _, out _);
        _readATraversal = _assets.TryRead("../secrets.json", out _, out _);
        _readAnAbsolutePath = _assets.TryRead("/assets/index.js", out _, out _);
    }

    [Fact] void should_read_an_embedded_asset() => _readTheScript.ShouldBeTrue();
    [Fact] void should_serve_the_content_that_was_embedded() => Encoding.UTF8.GetString(_script).ShouldEqual(Script);
    [Fact] void should_serve_it_as_what_its_extension_says_it_is() => _scriptContentType.ShouldEqual("text/javascript");
    [Fact] void should_read_the_document_the_explorer_is_served_from() => _readTheIndex.ShouldBeTrue();
    [Fact] void should_say_the_viewer_was_built_into_the_package() => _assets.HasIndex.ShouldBeTrue();
    [Fact] void should_not_read_a_resource_outside_the_viewer() => _readSomethingOutsideTheViewer.ShouldBeFalse();
    [Fact] void should_not_read_a_traversal() => _readATraversal.ShouldBeFalse();
    [Fact] void should_not_read_an_absolute_path() => _readAnAbsolutePath.ShouldBeFalse();

    static Assembly AnAssemblyWithAViewer()
    {
        var resources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{EmbeddedViewerAssets.ResourcePrefix}{EmbeddedViewerAssets.IndexPath}"] = "<html></html>",
            [$"{EmbeddedViewerAssets.ResourcePrefix}assets/index.js"] = Script,
            ["secrets.json"] = "{}"
        };

        var compilation = CSharpCompilation.Create(
            "Fixture.Viewer",
            [CSharpSyntaxTree.ParseText(string.Empty)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(
            stream,
            manifestResources:
            [
                .. resources.Select(resource => new ResourceDescription(
                    resource.Key,
                    () => new MemoryStream(Encoding.UTF8.GetBytes(resource.Value)),
                    isPublic: true))
            ]);

        return result.Success
            ? Assembly.Load(stream.ToArray())
            : throw new InvalidOperationException("The viewer fixture assembly could not be emitted.");
    }
}
