// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.given;

public class a_wrapper_initializer : Specification
{
    protected const string Definitions = @"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Arc.Chronicle.Streams;

[EventSource]
[EventStream(""Transactions"")]
[EventStream(""Adjustments"")]
public class AccountEventSource : IEventSource;
";

    protected static string Source(string initializer, string declaration = "[Command, Cratis.Arc.Chronicle.Commands.EventSource<AccountEventSource>(\"Transactions\")]", string creation = "new(\"id\", new object())") => Definitions + $@"
{declaration}
public record Deposit
{{
    public EventForEventSourceId Handle() => {creation} {initializer};
}}
";
}
