// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_SystemExecution.given;

public class a_system_execution : Specification
{
    protected SystemExecution _systemExecution;

    void Establish() => _systemExecution = new SystemExecution();
}
