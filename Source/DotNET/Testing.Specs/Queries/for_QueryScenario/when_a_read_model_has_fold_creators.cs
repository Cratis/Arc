// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_a_read_model_has_fold_creators : Specification
{
    readonly QueryScenario<FoldOnlyReadModel> _fold = new();
    readonly QueryScenario<OverloadedReadModel> _query = new();
    QueryResult _foldResult;
    QueryResult _hiddenResult;
    QueryResult _queryResult;

    async Task Because()
    {
        _foldResult = await _fold.Perform("Create");
        _hiddenResult = await _query.Perform("Helper");
        _queryResult = await _query.Perform("Create", new QueryArguments { ["id"] = "query" });
    }

    [Fact] void should_not_perform_a_fold_creator() => _foldResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_perform_an_opted_out_method() => _hiddenResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_perform_the_real_create_overload() => ((OverloadedReadModel)_queryResult.Data).Id.ShouldEqual("query");

    void Destroy()
    {
        _fold.Dispose();
        _query.Dispose();
    }
}
