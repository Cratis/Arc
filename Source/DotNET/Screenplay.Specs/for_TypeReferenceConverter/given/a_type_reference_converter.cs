// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;

namespace Cratis.Arc.Screenplay.for_TypeReferenceConverter.given;

public class a_type_reference_converter : Specification
{
    protected TypeReferenceConverter _converter;

    void Establish() => _converter = new(new ScreenplayNaming());
}
