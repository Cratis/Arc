// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.for_ArcOptions;

public class when_post_configuring_hosting : Specification
{
    string _applicationUrl;

    void Because()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Arc:Hosting:ApplicationUrl"] = "http://127.0.0.1:64321/"
            })
            .Build().GetSection("Cratis:Arc");
        var services = new ServiceCollection();
        ArcOptionsConfiguration.Configure(services, configuration);
        services.PostConfigure<ArcOptions>(options => options.Hosting.ApplicationUrl = "http://127.0.0.1:64322/");
        using var provider = services.BuildServiceProvider();
        _applicationUrl = provider.GetRequiredService<IOptions<ArcOptions>>().Value.Hosting.ApplicationUrl;
    }

    [Fact] void should_use_the_post_configured_application_url() => _applicationUrl.ShouldEqual("http://127.0.0.1:64322/");
}
