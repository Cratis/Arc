// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Arc.Mcp.for_McpOptIn;

public class when_inspecting_the_core_dependency_graph : Specification
{
    string[] _arc;
    string[] _core;
    string[] _mcpOwners;
    string[] _mcpVersions;

    void Because()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !IsRepositoryRoot(directory.FullName))
        {
            directory = directory.Parent;
        }
        directory.ShouldNotBeNull();
        var root = directory.FullName;
        _arc = Libraries(root, "Arc");
        _core = Libraries(root, "Arc.Core");
        var projects = XDocument.Load(Path.Combine(root, "Arc.slnx")).Descendants("Project")
            .Select(project => Resolve(root, project.Attribute("Path")!.Value)).ToArray();
        projects.Length.ShouldBeGreaterThan(0);
        _mcpOwners = projects.Where(project => Items(project).Any(item =>
                item.Name.LocalName == "PackageReference" && IsMcp(Identity(item))))
            .Select(Path.GetFileNameWithoutExtension).ToArray()!;
        _mcpVersions = Items(Path.Combine(root, "Source", "DotNET", "Arc.Mcp", "Arc.Mcp.csproj"))
            .Where(item => item.Name.LocalName == "PackageVersion" && IsMcp(Identity(item)))
            .Select(item => $"{Identity(item)}/{item.Attribute("Version")!.Value}").ToArray();
    }

    [Fact] void should_not_reference_mcp_directly_or_transitively_from_arc() => _arc.Any(IsMcp).ShouldBeFalse();
    [Fact] void should_not_reference_mcp_directly_or_transitively_from_core() => _core.Any(IsMcp).ShouldBeFalse();
    [Fact] void should_reference_the_sdk_only_from_the_optional_package() => _mcpOwners.ShouldEqual(["Arc.Mcp"]);
    [Fact] void should_confine_the_pinned_sdk_to_the_optional_package() => _mcpVersions.ShouldContain("ModelContextProtocol.AspNetCore/2.3.0");

    static bool IsRepositoryRoot(string directory) => File.Exists(Path.Combine(directory, "Arc.slnx")) && Path.Exists(Path.Combine(directory, ".git"));

    static bool IsMcp(string library) => library.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase) ||
        library.Equals("Cratis.Arc.Mcp", StringComparison.OrdinalIgnoreCase) || library.Equals("Arc.Mcp", StringComparison.OrdinalIgnoreCase);

    static string[] Libraries(string root, string project)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>();
        pending.Push(Path.Combine(root, "Source", "DotNET", project, $"{project}.csproj"));
        while (pending.TryPop(out var path))
        {
            if (!visited.Add(path))
            {
                continue;
            }
            libraries.Add(Path.GetFileNameWithoutExtension(path));
            var items = Items(path).ToArray();
            foreach (var reference in items.Where(item => item.Name.LocalName == "ProjectReference"))
            {
                pending.Push(Resolve(Path.GetDirectoryName(path)!, Identity(reference)));
            }
            libraries.UnionWith(items.Where(item => item.Name.LocalName == "PackageReference").Select(Identity));
        }
        visited.Count.ShouldBeGreaterThan(0);
        libraries.Count.ShouldBeGreaterThan(1);
        return libraries.ToArray();
    }

    static IEnumerable<XElement> Items(string project)
    {
        File.Exists(project).ShouldBeTrue();
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { project };
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(project)!); directory is not null; directory = directory.Parent)
        {
            foreach (var name in new[] { "Directory.Build.props", "Directory.Packages.props" })
            {
                var path = Path.Combine(directory.FullName, name);
                if (File.Exists(path))
                {
                    files.Add(path);
                }
            }
            if (File.Exists(Path.Combine(directory.FullName, "Arc.slnx")))
            {
                break;
            }
        }
        return files.SelectMany(file => ReadItems(file, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
    }

    static IEnumerable<XElement> ReadItems(string file, HashSet<string> visited)
    {
        // Inspect every declared condition conservatively, rather than depending on the build's active framework.
        if (!visited.Add(file))
        {
            return [];
        }
        var document = XDocument.Load(file);
        var directory = Path.GetDirectoryName(file)!;
        var imports = document.Descendants().Where(element => element.Name.LocalName == "Import")
            .Select(element => element.Attribute("Project")!.Value
                .Replace("$(MSBuildThisFileDirectory)", directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Select(import => Resolve(directory, import));
        return document.Descendants().Where(element => element.Name.LocalName == "ProjectReference" ||
                element.Name.LocalName == "PackageReference" || element.Name.LocalName == "PackageVersion")
            .Concat(imports.SelectMany(import => ReadItems(import, visited)));
    }

    static string Identity(XElement item) => (item.Attribute("Include") ?? item.Attribute("Update"))!.Value;

    static string Resolve(string directory, string path) => Path.GetFullPath(Path.Combine(directory, path.Replace('\\', Path.DirectorySeparatorChar)));
}
