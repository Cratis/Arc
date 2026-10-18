// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider;

public class with_opaque_dependencies : given.a_certified_validator
{
    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    public void should_refuse_reusable_dependencies_before_any_graph_constructor(ServiceLifetime lifetime)
    {
        _services.Add(new ServiceDescriptor(typeof(Opaque), typeof(Opaque), lifetime));
        Build();
        Catch.Exception(() => CertifiedProtectedValidatorConstruction.Construct(_scope.ServiceProvider, typeof(LateOpaqueValidator), true))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        _events.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_factories_and_instances_before_activating_any_branch(bool instance)
    {
        var factoryCalls = 0;
        if (instance)
        {
            _services.AddSingleton(new Opaque());
        }
        else
        {
            _services.AddTransient(_ =>
            {
                factoryCalls++;
                return new Opaque();
            });
        }
        Build();
        Catch.Exception(() => CertifiedProtectedValidatorConstruction.Construct(_scope.ServiceProvider, typeof(LateOpaqueValidator), true))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        factoryCalls.ShouldEqual(0);
        _events.ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_an_application_validator_self_binding()
    {
        _services.AddTransient<ReadingValidator>();
        Build();
        Catch.Exception(() => _validators.TryGet(typeof(Model), _scope.ServiceProvider, out _))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        _events.ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_the_unsupported_runtime_without_activating()
    {
        Build();
        Catch.Exception(() => CertifiedProtectedValidatorConstruction.Construct(_scope.ServiceProvider, typeof(ReadingValidator), false))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        _events.ShouldBeEmpty();
    }

    [Fact]
    public void should_keep_parameterless_certification_on_the_unsupported_runtime()
    {
        Build();
        var instance = ProtectedValidatorConstruction.Construct(typeof(EmptyValidator));
        ProtectedValidatorConstruction.IsCertified(instance, typeof(EmptyValidator)).ShouldBeTrue();
    }

    [Fact]
    public void should_refuse_a_direct_di_resolution_of_the_new_factory_before_callback()
    {
        var callbacks = 0;
        _services.AddProtectedDecisionValidator<ReadingValidator>(_ => callbacks++);
        Build();
        Catch.Exception(() => _scope.ServiceProvider.GetRequiredService<ReadingValidator>())
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        callbacks.ShouldEqual(0);
        _events.ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_without_an_activation_frame()
    {
        using var root = (ServiceProvider)_services.BuildProtectedDecisionServiceProvider();
        using var scope = root.CreateScope();
        using var policy = CommandDecisionPolicy.Begin(typeof(Model));
        Catch.Exception(() => _validators.TryGet(typeof(Model), scope.ServiceProvider, out _))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        _events.ShouldBeEmpty();
    }
}
