// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_installing_without_an_audience : given.an_installation
{
    void Establish()
    {
        Configure();
        _builder.ForResource("https://resource.example", " ", "test");
    }
    void Because() => _exception = Catch.Exception(() => _services.AddVerifiedCallers(_builder));
    [Fact] void should_refuse_the_installation() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
