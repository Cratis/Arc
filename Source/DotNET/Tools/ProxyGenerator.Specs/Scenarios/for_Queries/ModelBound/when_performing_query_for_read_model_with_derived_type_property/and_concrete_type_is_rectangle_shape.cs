// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound.when_performing_query_for_read_model_with_derived_type_property;

[Collection(ScenarioCollectionDefinition.Name)]
public class and_concrete_type_is_rectangle_shape : given.a_scenario_web_application
{
    QueryExecutionResult<DerivedTypeReadModel>? _executionResult;
    string? _shapeConstructorName;
    double _width;
    double _height;

    void Establish() => LoadQueryProxy<DerivedTypeReadModel>("GetWithRectangleShape");

    async Task Because()
    {
        _executionResult = await Bridge!.PerformQueryViaProxyAsync<DerivedTypeReadModel>("GetWithRectangleShape");
        _shapeConstructorName = Runtime!.Evaluate<string>("__queryResult?.data?.shape?.constructor?.name");
        _width = Convert.ToDouble(Runtime!.Evaluate<object>("__queryResult?.data?.shape?.width ?? 0"));
        _height = Convert.ToDouble(Runtime!.Evaluate<object>("__queryResult?.data?.shape?.height ?? 0"));
    }

    [Fact] void should_return_successful_result() => _executionResult!.Result!.IsSuccess.ShouldBeTrue();
    [Fact] void should_deserialize_shape_as_rectangle_shape() => _shapeConstructorName.ShouldEqual("RectangleShape");
    [Fact] void should_have_correct_width() => _width.ShouldEqual(10.0);
    [Fact] void should_have_correct_height() => _height.ShouldEqual(20.0);
}
