// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased;

/// <summary>
/// A command provider that is based on controllers.
/// </summary>
public class ControllerBasedArtifactsProvider : IArtifactsProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerBasedArtifactsProvider"/> class.
    /// </summary>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="outputPath">Output path for command.</param>
    /// <param name="segmentsToSkip">Segments to skip when generating output paths.</param>
    public ControllerBasedArtifactsProvider(
        Action<string> message,
        string outputPath,
        int segmentsToSkip)
    {
        var commands = new List<MethodInfo>();
        var queries = new List<MethodInfo>();

        message($"  Discover commands and queries from controllers from {TypeExtensions.Assemblies.Count()} assemblies");

        foreach (var controller in TypeExtensions.Assemblies.SelectMany(_ => _.DefinedTypes).Where(__ => __.IsController()))
        {
            var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            methods.Where(_ => _.IsQueryMethod()).ToList().ForEach(queries.Add);
            methods.Where(_ => _.IsCommandMethod()).ToList().ForEach(commands.Add);
        }

        Commands = commands.ConvertAll(_ => _.ToCommandDescriptor(outputPath, segmentsToSkip));
        Queries = queries.ConvertAll(_ => _.ToQueryDescriptor(outputPath, segmentsToSkip));
    }

    /// <inheritdoc/>
    public IEnumerable<CommandDescriptor> Commands { get; }

    /// <inheritdoc/>
    public IEnumerable<QueryDescriptor> Queries { get; }
}
