// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.given;

public class a_naming : Specification
{
    protected ScreenplayNaming _naming;

    void Establish() => _naming = new();
}
