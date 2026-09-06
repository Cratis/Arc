// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.EnumParameters;

/// <summary>
/// A status used to verify enum query parameters, plain and nullable, survive proxy generation.
/// </summary>
public enum Status
{
    /// <summary>
    /// Unknown status.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Active status.
    /// </summary>
    Active = 1
}

/// <summary>
/// A read model whose queries take an enum argument, plain and nullable.
/// </summary>
public class EnumParameterReadModel
{
    public string Name { get; set; } = string.Empty;

    public static IEnumerable<EnumParameterReadModel> GetByStatus(Status status) => [];

    public static IEnumerable<EnumParameterReadModel> GetByOptionalStatus(Status? status) => [];

    public static IEnumerable<EnumParameterReadModel> GetByStatusWithDependency(Status status, ISomeDependency dependency) => [];
}
