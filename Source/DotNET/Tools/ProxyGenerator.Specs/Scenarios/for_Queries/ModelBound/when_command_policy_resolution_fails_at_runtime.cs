// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_command_policy_resolution_fails_at_runtime : given.a_scenario_web_application
{
    [Theory]
    [InlineData("X-Unknown-Runtime-Policy")]
    [InlineData("X-Throw-Runtime-Policy")]
    public async Task should_not_expose_policy_provider_details_in_the_serialized_command_result(string failureHeader)
    {
        LoadCommandProxy<for_Commands.ModelBound.PolicyProtectedCommand>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
        HttpClient.DefaultRequestHeaders.Add(failureHeader, "yes");
        Host!.Services.GetRequiredService<IOptions<ArcOptions>>().Value.ExposeExceptionDetails.ShouldBeFalse();
        var result = (await Bridge!.ExecuteCommandViaProxyAsync<object>(new for_Commands.ModelBound.PolicyProtectedCommand())).Result;
        result.IsSuccess.ShouldBeFalse();
        var serialized = JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("ActiveSubscription");
        serialized.ShouldNotContain("SensitivePolicyProviderType");
        serialized.ShouldNotContain("Special are unavailable");
        result.AuthorizationFailureReason.ShouldBeEmpty();
    }
}
