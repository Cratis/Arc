// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders;

public class when_two_lookup_forms_are_declared : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ReadModelKeyProviders.Validate(typeof(Command).GetMethod(nameof(Command.Handle)).GetParameters()[0], typeof(Command)));

    [Fact] void should_refuse_the_declaration() => _exception.ShouldBeOfExactType<InvalidLookUpBy>();

    class Command
    {
        public string Id { get; set; }
        public void Handle([LookUpBy(nameof(Id)), LookUpBy<CallerSubject>] object model) { }
    }
}
