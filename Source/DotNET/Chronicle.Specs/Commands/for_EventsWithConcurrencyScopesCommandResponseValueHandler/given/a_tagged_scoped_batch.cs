// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.given;

public abstract class a_tagged_scoped_batch : Commands.given.a_named_tag_append
{
    protected override ICommandResponseValueHandler CreateHandler() => new EventsWithConcurrencyScopesCommandResponseValueHandler(_eventLog);
    protected override object CreateValue() => new EventsWithConcurrencyScopes([Wrapper()], []);
}
