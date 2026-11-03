// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// An application replaces an option object with a subclass that adds its own property.
/// </summary>
public class when_binding_a_subclass_of_an_option_type : Specification
{
    ArcOptions _options;
    SubclassedOnBehalfOfOptions _subclass;
    int _bindCount;

    void Establish()
    {
        _subclass = new SubclassedOnBehalfOfOptions();
        _options = new ArcOptions { OnBehalfOf = _subclass };
        RuntimeTypeFallback.ResetBindCount();
    }

    void Because()
    {
        ArcOptionsBinder.Bind(
            _options,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cratis:Arc:OnBehalfOf:Issuer"] = "issuer",
                    ["Cratis:Arc:OnBehalfOf:Extra"] = "extra",
                    ["Cratis:Arc:OnBehalfOf:ExtraList:0"] = "a",
                    ["Cratis:Arc:OnBehalfOf:SigningKeys:0:KeyId"] = "k0"
                })
                .Build().GetSection("Cratis:Arc"));
        _bindCount = RuntimeTypeFallback.BindCount;
    }

    [Fact] void should_run_the_fallback_once() => _bindCount.ShouldEqual(1);

    [Fact] void should_bind_the_property_of_the_subclass() => _subclass.Extra.ShouldEqual("extra");

    [Fact] void should_bind_the_list_of_the_subclass() => _subclass.ExtraList.Single().ShouldEqual("a");

    [Fact] void should_bind_the_declared_property() => _subclass.Issuer.ShouldEqual("issuer");

    [Fact] void should_not_duplicate_a_declared_list() => _subclass.SigningKeys.Count.ShouldEqual(1);

    class SubclassedOnBehalfOfOptions : OnBehalfOfOptions
    {
        public string Extra { get; set; } = string.Empty;

        public IList<string> ExtraList { get; set; } = [];
    }
}
