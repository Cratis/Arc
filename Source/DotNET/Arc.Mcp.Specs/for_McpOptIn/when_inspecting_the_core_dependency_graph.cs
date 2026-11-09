// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Mcp.for_McpOptIn;

public class when_inspecting_the_core_dependency_graph : Specification
{
    string[] _arc;
    string[] _core;
    string[] _mcp;

    void Because()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }
        directory.ShouldNotBeNull();
        _arc = Libraries(directory.FullName, "Arc");
        _core = Libraries(directory.FullName, "Arc.Core");
        _mcp = Libraries(directory.FullName, "Arc.Mcp");
    }

    [Fact] void should_not_reference_mcp_directly_or_transitively_from_arc() => _arc.Any(IsMcp).ShouldBeFalse();
    [Fact] void should_not_reference_mcp_directly_or_transitively_from_core() => _core.Any(IsMcp).ShouldBeFalse();
    [Fact] void should_confine_the_pinned_sdk_to_the_optional_package() => _mcp.ShouldContain("ModelContextProtocol/2.3.0");

    static bool IsMcp(string library) => library.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase) ||
        library.StartsWith("Cratis.Arc.Mcp/", StringComparison.OrdinalIgnoreCase) || library.StartsWith("Arc.Mcp/", StringComparison.OrdinalIgnoreCase);

    static string[] Libraries(string root, string project)
    {
        using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Source", "DotNET", project, "obj", "project.assets.json")));
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToArray();
        libraries.Length.ShouldBeGreaterThan(0);
        return libraries;
    }
}
