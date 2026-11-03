// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Runtime subtypes of leaf values bind their own properties through the stock binder.
/// </summary>
public class when_binding_a_uri_subtype : given_a_parity_run
{
    readonly List<CustomUri> _endpoints = [];

    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Endpoint:Extra"] = "configured"
    };

    protected override Action<ArcOptions> Prepare => options =>
    {
        var endpoint = new CustomUri();
        _endpoints.Add(endpoint);
        options.ConnectionResolution.Identity.Http.Endpoint = endpoint;
    };

    [Fact] void should_bind_the_subclass_property() => _endpoints[1].Extra.ShouldEqual("configured");

    [Fact] void should_bind_the_subclass_property_like_the_stock_binder() => _endpoints[1].Extra.ShouldEqual(_endpoints[0].Extra);

    class CustomUri() : Uri("https://example.com")
    {
        public string Extra { get; set; } = "initial";
    }
}
