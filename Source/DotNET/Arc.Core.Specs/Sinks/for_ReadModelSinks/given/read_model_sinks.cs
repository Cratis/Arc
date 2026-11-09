// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Sinks.for_ReadModelSinks.given;

public class read_model_sinks : Specification
{
    protected IReadModelSinkNameProvider _first;
    protected IReadModelSinkNameProvider _second;
    protected ReadModelSinks _sinks;

    void Establish()
    {
        _first = Substitute.For<IReadModelSinkNameProvider>();
        _second = Substitute.For<IReadModelSinkNameProvider>();
        _sinks = new([_first, _second]);
    }
}
