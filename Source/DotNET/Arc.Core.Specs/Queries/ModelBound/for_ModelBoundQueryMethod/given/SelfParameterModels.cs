// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // Related self-parameter fixtures shared with proxy specifications.
#pragma warning disable SA1649 // Related self-parameter fixtures shared with proxy specifications.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

[ReadModel]
public record SelfParameterModel
{
    public static SelfParameterModel First(SelfParameterModel value, object service) => value;
    public static SelfParameterModel Second(object @event, SelfParameterModel current) => current;
    public static SelfParameterModel? Reduce(object @event, SelfParameterModel? current) => current;
    public static Task<SelfParameterModel?> Last(object @event, object context, SelfParameterModel? anything) => Task.FromResult(anything);
    public static SelfParameterModel Collection(IEnumerable<SelfParameterModel> values) => values.First();
    public static SelfParameterModel Other(ConventionalReadModel model) => new();
    public static SelfParameterModel Service(object service) => new();
    public static SelfParameterModel All() => new();
}

public record struct SelfParameterValue
{
    public static SelfParameterValue Fold(SelfParameterValue? value) => value ?? default;
    public static SelfParameterValue All() => default;
}
