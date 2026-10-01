// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Framework;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.given;

/// <summary>
/// A build engine that records what a task said to it.
/// </summary>
/// <remarks>
/// MSBuild hands a task the engine, so specifying a task means standing in for one. Recording rather than
/// asserting keeps what was reported available to the specification, which is the half that knows what should
/// have been said.
/// </remarks>
public class a_build : IBuildEngine
{
    /// <summary>
    /// Gets everything the task reported as an error.
    /// </summary>
    public List<BuildErrorEventArgs> Errors { get; } = [];

    /// <summary>
    /// Gets everything the task reported as a warning.
    /// </summary>
    public List<BuildWarningEventArgs> Warnings { get; } = [];

    /// <summary>
    /// Gets everything the task reported as a message.
    /// </summary>
    public List<BuildMessageEventArgs> Messages { get; } = [];

    /// <inheritdoc/>
    public bool ContinueOnError => false;

    /// <inheritdoc/>
    public int LineNumberOfTaskNode => 0;

    /// <inheritdoc/>
    public int ColumnNumberOfTaskNode => 0;

    /// <inheritdoc/>
    public string ProjectFileOfTaskNode => "specification.csproj";

    /// <inheritdoc/>
    public bool BuildProjectFile(
        string projectFileName,
        string[] targetNames,
        System.Collections.IDictionary globalProperties,
        System.Collections.IDictionary targetOutputs) => true;

    /// <inheritdoc/>
    public void LogCustomEvent(CustomBuildEventArgs e)
    {
    }

    /// <inheritdoc/>
    public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);

    /// <inheritdoc/>
    public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);

    /// <inheritdoc/>
    public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
}
