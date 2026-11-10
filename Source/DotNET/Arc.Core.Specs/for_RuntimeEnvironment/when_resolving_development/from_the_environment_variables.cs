// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Execution.for_RuntimeEnvironment.when_resolving_development;

public class from_the_environment_variables : Specification
{
    bool _onlyDotNetEnvironment;
    bool _bothWithAspNetCoreWinning;
    bool _aspNetCoreProduction;
    bool _neither;

    void Because()
    {
        _onlyDotNetEnvironment = RuntimeEnvironment.IsDevelopmentIn(null, "Development");
        _bothWithAspNetCoreWinning = RuntimeEnvironment.IsDevelopmentIn("Development", "Production");
        _aspNetCoreProduction = RuntimeEnvironment.IsDevelopmentIn("Production", "Development");
        _neither = RuntimeEnvironment.IsDevelopmentIn(null, null);
    }

    [Fact] void should_honor_dotnet_environment_when_aspnetcore_environment_is_not_set() => _onlyDotNetEnvironment.ShouldBeTrue();
    [Fact] void should_let_aspnetcore_environment_select_development() => _bothWithAspNetCoreWinning.ShouldBeTrue();
    [Fact] void should_let_aspnetcore_environment_win_over_dotnet_environment() => _aspNetCoreProduction.ShouldBeFalse();
    [Fact] void should_not_be_development_when_neither_is_set() => _neither.ShouldBeFalse();
}
