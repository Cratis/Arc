// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_handlers_without_a_response : a_generated_document
{
    [Theory]
    [InlineData("public Task Handle(Author author) => author.Register(Name);")]
    [InlineData("public Task Handle(IEventLog log) => log.Append(Id, new AuthorRegistered(Name));")]
    [InlineData("public async Task Handle(IEventLog log) => await log.Append(Id, new AuthorRegistered(Name));")]
    [InlineData("public Task<AppendResult> Handle(IEventLog log) => log.Append(Id, new AuthorRegistered(Name));")]
    [InlineData("public async Task<AppendResult> Handle(IEventLog log) => await log.Append(Id, new AuthorRegistered(Name));")]
    [InlineData("public Task<AuthorRegistered> Handle() => Task.FromResult(new AuthorRegistered(Name));")]
    [InlineData("public ValueTask<AuthorRegistered> Handle() => new(new AuthorRegistered(Name));")]
    [InlineData("public ValueTask Handle() => ValueTask.CompletedTask;")]
    [InlineData("public ValueTask<AppendResult> Handle(IEventLog log) => new(log.Append(Id, new AuthorRegistered(Name)));")]
    [InlineData("public void Handle() => System.Console.WriteLine(Name);")]
    [InlineData("public IEnumerable<object> Handle() { return [new AuthorRegistered(Name), new AuthorInvited(Name)]; }")]
    [InlineData("public IEnumerable<object> Handle() => new object[] { new AuthorRegistered(Name), new AuthorInvited(Name) };")]
    public void should_not_report_an_unreadable_response(string handler)
    {
        Generate((Analyzed.SlicePath, $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.EventSequences;
            using Cratis.Chronicle.Keys;
            namespace Library.Authors.Registration;
            [EventType] public record AuthorRegistered(string Name);
            [EventType] public record AuthorInvited(string Name);
            public class Author { public Task Register(string name) => Task.CompletedTask; }
            [Command] public record RegisterAuthor([Key] Guid Id, string Name)
            {
                {{handler}}
            }
            """));

        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeEmpty();
        Result.Source.ShouldNotContain("returns");
        AssertDocument();
    }
}
