// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_a_formattable_concept : given.a_command_event_tags_resolver
{
    CultureInfo _originalCulture;
    void Establish()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    }
    void Because() => _tags = Resolve(new TaggedCommand(new Amount(12.5m)));
    void Destroy() => CultureInfo.CurrentCulture = _originalCulture;

    [Fact] void should_render_using_invariant_culture() => _tags.ShouldEqual([new NamedTag("amount", "12.5")]);

    record Amount(decimal Value) : ConceptAs<decimal>(Value);
    [EventTag("amount", nameof(Amount))]
    record TaggedCommand(Amount Amount);
}
