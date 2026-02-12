// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandValidationRouteConvention.given;

public class a_command_validation_route_convention : Specification
{
    protected CommandValidationRouteConvention _convention;

    void Establish() => _convention = new CommandValidationRouteConvention();
}
