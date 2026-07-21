// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels;

public class SearchByEmailParameters
{
    public string Email { get; set; } = string.Empty;

    public int MinAge { get; set; }
}
