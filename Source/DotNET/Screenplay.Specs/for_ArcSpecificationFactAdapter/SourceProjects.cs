// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

/// <summary>
/// Creates project-aware source contexts for neutral Arc adapter specifications.
/// </summary>
public static class SourceProjects
{
    /// <summary>
    /// Creates one project with every authored syntax tree mapped to stable source identity.
    /// </summary>
    /// <param name="name">The logical project name.</param>
    /// <param name="role">The role of the project.</param>
    /// <param name="compilation">The project compilation.</param>
    /// <param name="projectIdentity">The optional stable project identity.</param>
    /// <param name="relativePathFor">The optional project-relative path mapping.</param>
    /// <returns>The mapped project compilation.</returns>
    public static DotNetProjectCompilation Create(
        string name,
        DotNetProjectRole role,
        Compilation compilation,
        string? projectIdentity = null,
        Func<SyntaxTree, string>? relativePathFor = null)
    {
        var authoredTrees = compilation.SyntaxTrees.ToHashSet();
        var documents = authoredTrees.Select(tree =>
        {
            var relativePath = relativePathFor?.Invoke(tree) ?? tree.FilePath;
            return new DotNetSourceDocument
            {
                SyntaxTree = tree,
                ProjectRelativePath = relativePath,
                WorkspaceRelativePath = $"{name}/{relativePath}"
            };
        }).ToArray();
        var sourceContext = DotNetSourcePaths.Create(
            projectIdentity ?? name,
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            documents);

        return new()
        {
            Name = name,
            Role = role,
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = authoredTrees
        };
    }
}
