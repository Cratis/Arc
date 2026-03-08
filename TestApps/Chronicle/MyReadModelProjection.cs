// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

namespace Chronicle;

public class MyReadModelProjection : IProjectionFor<MyReadModel>
{
    public void Define(IProjectionBuilderFor<MyReadModel> builder) => builder
        .From<MyEvent>();
}