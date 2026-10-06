// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Declares commands whose productions do not copy the source identity into the event payload.
/// </summary>
public static class IdentifierSources
{
    /// <summary>
    /// Creates a source file around the supplied command declaration.
    /// </summary>
    /// <param name="command">The command declaration.</param>
    /// <returns>The C# source.</returns>
    public static string With(string command) => """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;

        namespace Library.Authors.Registration;

        public record AuthorId(Guid Value) : EventSourceId<Guid>(Value);

        [EventType]
        public record AuthorRegistered(string Name);

        """ + command;
}
