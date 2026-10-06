// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Declares a command reaching the same aggregate behavior through different destinations.
/// </summary>
public static class AggregateRoutingSources
{
    /// <summary>
    /// Creates a source file with the supplied handler.
    /// </summary>
    /// <param name="handler">The handler declaration.</param>
    /// <returns>The source.</returns>
    public static string With(string handler) => """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        namespace Library.Authors.Registration;
        [EventType]
        public record AuthorRegistered(string Name);
        public class Author : AggregateRoot
        {
            public Task Register(string name) => Apply(new AuthorRegistered(name));
            public void OnRegistered(AuthorRegistered @event) { }
        }
        [Command]
        public record RegisterAuthor([Key] Guid Id, Guid OtherId, string Name)
        {
        """ + handler + "}";
}
