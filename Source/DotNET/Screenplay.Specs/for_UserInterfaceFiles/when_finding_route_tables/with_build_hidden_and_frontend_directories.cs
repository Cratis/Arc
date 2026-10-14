// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.for_UserInterfaceFiles.when_finding_route_tables;

public class with_build_hidden_and_frontend_directories : Specification
{
    string _root;
    string[] _found;
    string[] _screens;
    string[] _excluded;

    void Establish()
    {
        var repository = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(repository, ".git")) && !Directory.Exists(Path.Combine(repository, ".git")))
        {
            repository = Path.GetDirectoryName(repository.TrimEnd(Path.DirectorySeparatorChar))!;
        }

        _root = Path.Combine(repository, ".ai-work", "3203", $"route-discovery-{Guid.NewGuid():N}");
        foreach (var directory in new[] { string.Empty, "src/routing", ".frontend/routing", "node_modules", "bin", "obj", "dist", "wwwroot", ".hidden", "src/.hidden", "src/node_modules", "src/bin", "src/obj", "src/dist", "src/wwwroot" })
        {
            var path = Path.Combine(_root, directory);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "App.tsx"), string.Empty);
        }

        File.CreateSymbolicLink(Path.Combine(_root, "Linked.tsx"), Path.Combine(_root, "node_modules", "App.tsx"));
        Directory.CreateSymbolicLink(Path.Combine(_root, "src", "linked"), Path.Combine(_root, ".hidden"));
    }

    void Because()
    {
        var files = new UserInterfaceFiles();
        _found = files.Under(_root).Select(_ => Path.GetRelativePath(_root, _).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        _screens = files.In(Path.Combine(_root, "src")).ToArray();
        _excluded = files.Under(Path.Combine(_root, "node_modules")).ToArray();
    }

    void Destroy() => Directory.Delete(_root, recursive: true);

    [Fact] void should_include_only_source_and_frontend_directories_and_not_symlinks() => _found.ShouldEqual([".frontend/routing/App.tsx", "App.tsx", "src/routing/App.tsx"]);
    [Fact] void should_keep_screen_discovery_top_directory_only() => _screens.ShouldBeEmpty();
    [Fact] void should_exclude_a_dependency_directory_even_as_the_search_root() => _excluded.ShouldBeEmpty();
}
