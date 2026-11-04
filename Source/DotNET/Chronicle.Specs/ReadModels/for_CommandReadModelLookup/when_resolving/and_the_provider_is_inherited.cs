// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup.when_resolving;

public class and_the_provider_is_inherited : given.a_caller_lookup
{
    void Establish() => _context = _context with { Type = typeof(DerivedCommand), Command = new DerivedCommand() };

    void Because() => Resolve<DerivedCommand>();

    [Fact] void should_use_the_caller_in_every_mode() => _keys.ShouldEachConformTo(key => key.Value == "selected");

    class BaseCommand
    {
        public string Id { get; set; } = "client-key";
        public virtual void Handle([LookUpBy<CallerSubject>] Model model) { }
    }

    class DerivedCommand : BaseCommand
    {
        public override void Handle(Model model) { }
    }
}
