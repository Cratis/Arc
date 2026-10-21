// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionRequest;

public class when_getting_the_key : Specification
{
    ConnectionRequest _first;
    ConnectionRequest _second;
    ConnectionRequest _otherPurpose;

    void Establish()
    {
        _first = new("SqlServer", ConnectionPurpose.Runtime, new Dictionary<string, string> { ["eventStore"] = "orders", ["namespace"] = "acme" });
        _second = new("SqlServer", ConnectionPurpose.Runtime, new Dictionary<string, string> { ["namespace"] = "acme", ["eventStore"] = "orders" });
        _otherPurpose = _first with { Purpose = ConnectionPurpose.Schema };
    }

    [Fact] void should_not_depend_on_scope_order() => _first.ToKey().ShouldEqual(_second.ToKey());
    [Fact] void should_depend_on_purpose() => _first.ToKey().ShouldNotEqual(_otherPurpose.ToKey());
}
