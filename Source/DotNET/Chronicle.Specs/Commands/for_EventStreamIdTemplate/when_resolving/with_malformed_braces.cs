// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_malformed_braces : given.a_stream_id_template
{
    static readonly string[] _malformed = ["{Scope", "Scope}", "{}", "{Scope{Period}}"];
    readonly List<Exception?> _errors = [];

    void Because()
    {
        foreach (var template in _malformed)
        {
            _errors.Add(Catch.Exception(() => EventStreamIdTemplate.Resolve(template, new Composite(new("owner"), new(2026, 1, 2)))));
        }
    }

    [Fact] void should_refuse_every_malformed_template() => _errors.TrueForAll(error => error is InvalidEventStreamIdTemplate).ShouldBeTrue();
}
