// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_both_lookup_forms_are_present : given.a_caller_lookup
{
    void Because() => Refuse<MixedCommand>();

    [Fact] void should_reject_the_declaration_in_every_mode() => _exceptions.ShouldEachConformTo(exception => exception is InvalidLookUpBy);

    class MixedCommand
    {
        public void Handle([LookUpBy("Id"), LookUpBy<CallerSubject>] Model model) { }
    }
}
