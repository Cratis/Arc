// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;

public class an_emitter : Specification
{
    protected ScreenplayEmitter _emitter;
    protected ScreenplayOptions _options;

    void Establish()
    {
        _emitter = new();
        _options = new();
    }
}
