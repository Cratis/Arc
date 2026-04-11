// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_ChangeSetComputor.given;

public class a_change_set_computor : Specification
{
    protected ChangeSetComputor _computor;

    void Establish() =>
        _computor = new ChangeSetComputor(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
}
