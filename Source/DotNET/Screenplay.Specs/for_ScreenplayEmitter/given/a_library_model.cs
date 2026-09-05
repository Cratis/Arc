// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;

public class a_library_model : an_emitter
{
    protected ApplicationModel _model;

    void Establish() => _model = LibraryApplication.Build();
}
