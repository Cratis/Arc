// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the named subjects of the compiler's protected-read diagnostics, independently of their wording and location.
/// </summary>
internal static class ProtectedReadDiagnostic
{
    static readonly string[] _subjectCodes = ["PLAY0781", "PLAY0782", "PLAY0783", "PLAY0784", "PLAY0785"];

    /// <summary>
    /// Determines whether a diagnostic declares protected-read subjects.
    /// </summary>
    /// <param name="diagnostic">The compiler diagnostic.</param>
    /// <returns>True for codes with typed command and read subjects.</returns>
    internal static bool HasSubjects(Diagnostic diagnostic) => _subjectCodes.Contains(diagnostic.Code);

    /// <summary>
    /// Attributes a diagnostic to its declared command and reads.
    /// </summary>
    /// <param name="application">The parsed document.</param>
    /// <param name="diagnostic">The compiler diagnostic with named subjects.</param>
    /// <returns>The reads concerned, empty when the subjects do not resolve.</returns>
    internal static IEnumerable<ProtectedReadRefusal> Attribute(ApplicationSyntax? application, Diagnostic diagnostic)
    {
        if (application is null || !diagnostic.Arguments.TryGetValue("command", out var command))
        {
            return [];
        }

        var path = command.Value.GetProperty("path").EnumerateArray().Select(segment => segment.GetString()).ToArray();
        var commands = application.Modules.SelectMany(module => module.Features.SelectMany(feature => Commands(feature, [module.Name])));

        return commands.Where(placed => placed.Path.SequenceEqual(path))
            .SelectMany(placed => Concerned(placed.Command, diagnostic)
                .Select(read => new ProtectedReadRefusal(placed.Slice.Name, placed.Command.Name, read.Alias ?? read.ReadModel, diagnostic)));
    }

    /// <summary>
    /// Formats a refusal reason without parsing the compiler's message.
    /// </summary>
    /// <param name="diagnostic">The compiler diagnostic.</param>
    /// <returns>The existing reason spelling, or an unknown reason's raw keyword.</returns>
    internal static string Message(Diagnostic diagnostic)
    {
        if (diagnostic.Code != "PLAY0781")
        {
            return diagnostic.Message;
        }

        var reason = diagnostic.Arguments["reason"].Value.GetString();
        var description = reason switch
        {
            "reducer" => "Reducer",
            "join" => "Join",
            "children" => "Children",
            "open-subscription" => "OpenSubscription",
            "payload-key" => "PayloadKey",
            "by-parts" => "ByParts",
            "no-removal" => "NoRemoval",
            "empty-handled-set" => "EmptyHandledSet",
            "named-source-or-stream" => "NamedSourceOrStream",
            "singleton" => "Singleton",
            "key-type" => "KeyType",
            "generated-key" => "GeneratedKey",
            "optional-key" => "OptionalKey",
            "collection-key" => "CollectionKey",
            _ => reason
        };
        var readModel = diagnostic.Arguments["readModel"].Value.GetProperty("path").EnumerateArray().Last().GetString();

        return $"Read '{readModel}' cannot be protected: {description}.";
    }

    static IEnumerable<ReadsSyntax> Concerned(CommandSyntax command, Diagnostic diagnostic)
    {
        if (diagnostic.Code == "PLAY0783")
        {
            return command.Reads ?? [];
        }

        if (!diagnostic.Arguments.TryGetValue("read", out var subject))
        {
            return [];
        }

        var name = subject.Value.GetString();

        return (command.Reads ?? []).Where(read => (read.Alias ?? read.ReadModel) == name || (diagnostic.Code == "PLAY0785" && read.ReadModel == name));
    }

    static IEnumerable<(string[] Path, SliceSyntax Slice, CommandSyntax Command)> Commands(FeatureSyntax feature, string[] parents)
    {
        string[] path = [.. parents, feature.Name];

        return feature.Slices.SelectMany(slice => slice.Commands.Select(command => (Path: (string[])[.. path, slice.Name, command.Name], Slice: slice, Command: command)))
            .Concat(feature.Features.SelectMany(child => Commands(child, path)));
    }
}
