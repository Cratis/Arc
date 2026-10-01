// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public partial class when_serving_the_built_viewer : a_host
{
    string _index;
    string[] _assets;
    HttpResponseMessage[] _responses;

    protected override IReadOnlyList<Assembly> Assemblies => [typeof(Company.Library.Program).Assembly];

    async Task Because()
    {
        _index = await _client.GetStringAsync(Url("/"));
        _assets = [.. AssetLinks().Matches(_index)
            .Select(_ => _.Groups["asset"].Value)];
        _responses = await Task.WhenAll(_assets.Select(_ => _client.GetAsync(Url($"/{_}"))));
    }

    [GeneratedRegex("(?:src|href)=\"\\./(?<asset>assets/[^\"]+)\"", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex AssetLinks();

    [Fact] void should_serve_the_built_application() => _index.Contains("id=\"root\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_check_actual_bundle_assets_instead_of_passing_on_an_empty_set() => _assets.Length.ShouldBeGreaterThan(1);
    [Fact] void should_serve_every_asset_referenced_by_the_index() => _responses.All(_ => _.StatusCode == HttpStatusCode.OK).ShouldBeTrue();
}
