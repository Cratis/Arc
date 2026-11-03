// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

public class when_configuring_artifact_opt_ins : Specification
{
    ArcOptions _options;

    void Establish()
    {
        _options = new ArcOptions();
        _options.Artifacts.Include(typeof(ArcOptions).Assembly);
    }

    void Because() => ArcOptionsConfiguration.Bind(
        _options,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Artifacts:IncludedAssemblies:0"] = typeof(when_configuring_artifact_opt_ins).Assembly.FullName
        }).Build().GetSection("Cratis:Arc"));

    [Fact]
    void should_preserve_only_the_code_configured_opt_ins() =>
        _options.Artifacts.IncludedAssemblies.ShouldEqual<System.Reflection.Assembly>([typeof(ArcOptions).Assembly]);
}
