// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.HttpMethods;

public class MethodDecorated
{
    public string Id { get; set; } = string.Empty;

    [QueryHttpMethod(QueryHttpMethod.Query)]
    public static MethodDecorated Get(string id) => new() { Id = id };
}

[QueryHttpMethod(QueryHttpMethod.Auto)]
public class TypeDecorated
{
    public string Id { get; set; } = string.Empty;

    public static TypeDecorated Get(string id) => new() { Id = id };
}

[QueryHttpMethod(QueryHttpMethod.Auto)]
public class BothDecorated
{
    public string Id { get; set; } = string.Empty;

    [QueryHttpMethod(QueryHttpMethod.Query)]
    public static BothDecorated Get(string id) => new() { Id = id };
}

public class NotDecorated
{
    public string Id { get; set; } = string.Empty;

    public static NotDecorated Get(string id) => new() { Id = id };
}
