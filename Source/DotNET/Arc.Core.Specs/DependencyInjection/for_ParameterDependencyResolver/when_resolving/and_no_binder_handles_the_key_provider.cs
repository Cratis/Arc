// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver.when_resolving;

public class and_no_binder_handles_the_key_provider : Specification
{
    ServiceProvider _services;
    Exception?[] _exceptions;
    bool _activated;

    void Establish() => _services = new ServiceCollection()
        .AddTransient(_ =>
        {
            _activated = true;
            return new MongoModel();
        })
        .AddTransient(_ =>
        {
            _activated = true;
            return new EntityModel();
        })
        .BuildServiceProvider();

    void Because() => _exceptions = typeof(Command).GetMethod("Handle")!.GetParameters().Select(parameter =>
        Catch.Exception(() => ParameterDependencyResolver.Resolve(_services, parameter, _ => new InvalidOperationException()))).ToArray();

    void Destroy() => _services.Dispose();

    [Fact] void should_refuse_every_unsupported_lookup() => _exceptions.ShouldEachConformTo(exception => exception is InvalidLookUpBy);
    [Fact] void should_not_activate_a_type_based_registration() => _activated.ShouldBeFalse();

    class Command
    {
        public void Handle([LookUpBy<CallerSubject>] MongoModel mongo, [LookUpBy<CallerSubject>] EntityModel? entity) { }
    }

    class MongoModel;
    class EntityModel;
}
