// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

[ReadModel(RequireQueryAttribute = true)]
public abstract record ExplicitAbstractReadModel
{
    [Query]
    public static ExplicitAbstractReadModel? All() => null;
}
