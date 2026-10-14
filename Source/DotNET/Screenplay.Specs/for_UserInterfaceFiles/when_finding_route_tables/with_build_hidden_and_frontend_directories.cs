// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.for_UserInterfaceFiles.when_finding_route_tables;

public class with_build_hidden_and_frontend_directories : Specification
{
    string _root;
    string[] _found;

    void Establish()
    {
        var repository = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(repository, ".git")) && !Directory.Exists(Path.Combine(repository, ".git")))
        {
            repository = Path.GetDirectoryName(repository.TrimEnd(Path.DirectorySeparatorChar))!;
        }

        _root = Path.Combine(repository, ".ai-work", "3203", $"route-discovery-{Guid.NewGuid():N}");
        foreach (var directory in new[] { string.Empty, "src/routing", ".frontend/routing", "node_modules", "bin", "obj", "dist", "wwwroot", ".hidden", "src/.hidden" })
        {
            var path = Path.Combine(_root, directory);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "App.tsx"), string.Empty);
        }
    }

    void Because() => _found = new UserInterfaceFiles().Under(_root).Select(_ => Path.GetRelativePath(_root, _).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();

    void Destroy() => Directory.Delete(_root, recursive: true);

    [Fact] void should_include_only_source_and_frontend_directories() => _found.ShouldEqual([".frontend/routing/App.tsx", "App.tsx", "src/routing/App.tsx"]);
}
