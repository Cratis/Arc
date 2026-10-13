// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// A project returned by a command.
/// </summary>
/// <param name="Name">The project name.</param>
public record ProjectSummary(string Name);

/// <summary>
/// A command returning a collection of projects.
/// </summary>
public record SummarizeProjects
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <returns>The project summaries.</returns>
    public IEnumerable<ProjectSummary> Handle() => [];
}

/// <summary>
/// A command returning a collection of objects.
/// </summary>
public record CollectObjects
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <returns>The objects.</returns>
    public IEnumerable<object> Handle() => [];
}

/// <summary>
/// A command returning a single project.
/// </summary>
public record SummarizeProject
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <returns>The project summary.</returns>
    public ProjectSummary Handle() => new("Arc");
}
