// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Chronicle.Slices.for_Slice.given;

[Command]
[Roles("Administrator")]
public record SliceAdministration
{
    public string Handle() => "Completed";
}
