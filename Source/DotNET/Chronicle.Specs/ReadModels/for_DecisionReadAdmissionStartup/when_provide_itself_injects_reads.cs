// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.ProvidedDeclarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_provide_itself_injects_reads : given.a_startup
{
    void Establish()
    {
        _reader.Describe<Model>().Returns(new DecisionReadDescription(new(false, DecisionReadRefusalReason.Hierarchy), [], false));
        Configure(typeof(InjectedProvideRead), typeof(InjectedProvideToken));
    }

    Task Because() => StartAndCheck();

    [Fact] void should_check_plain_model_dependencies_under_on() => Logged(LogLevel.Critical, typeof(InjectedProvideRead).FullName!).ShouldBeTrue();
    [Fact] void should_check_wrapped_dependencies() => Logged(LogLevel.Critical, typeof(InjectedProvideToken).FullName!).ShouldBeTrue();
    [Fact] void should_describe_the_plain_dependency_once() => _reader.Received(1).Describe<given.Declarations.Balance>();
    [Fact] void should_describe_the_wrapped_dependency_once() => _reader.Received(1).Describe<Model>();
    [Fact] void should_stop_the_host() => _lifetime.Received(1).StopApplication();
}
