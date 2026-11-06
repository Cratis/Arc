// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_installing_twice : given.an_installation
{
    int _count;
    void Establish()
    {
        Configure();
        _services.AddVerifiedCallers(_builder);
        _count = _services.Count;
        _builder.ForResource("https://other.example", "other-audience", "other-environment");
    }
    void Because() => _exception = Catch.Exception(() => _services.AddVerifiedCallers(_builder));
    [Fact] void should_refuse_a_second_installation() => _exception.ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
    [Fact] void should_leave_the_first_installation_unchanged() => _services.Count.ShouldEqual(_count);
}
