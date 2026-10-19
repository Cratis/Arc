// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Concepts;
using Cratis.Execution;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_a_generated_value : for_DiscoverableValidators.when_using_a_certified_provider.given.a_certified_validator
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task should_own_generated_value_validation_before_capturing_the_value(bool reject, bool failCleanup)
    {
        _types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(IdValidator)]);
        _services.AddSingleton<IDiscoverableValidators>(new DiscoverableValidators(_types));
        var callbacks = 0;
        _services.AddProtectedDecisionValidator<IdValidator>(validator =>
        {
            callbacks++;
            validator.FailCleanup = failCleanup;
            validator.RuleFor(id => id.Value).MustAsync((_, _) => Task.FromResult(!reject)).WithMessage("generated value denied");
        });
        _root = _services.BuildProtectedDecisionServiceProvider();
        await using var scope = _root.CreateAsyncScope();
        _policy = CommandDecisionPolicy.Begin(typeof(Model));
        ProtectedValidatorActivationFrame.Current.ShouldBeNull();
        GeneratedValueFactories.Register(typeof(Id), Id.New);
        var command = new GeneratesId();
        var context = new CommandContext(CorrelationId.New(), typeof(GeneratesId), command, [], new());
        var parameter = typeof(GeneratesId).GetMethod(nameof(GeneratesId.Handle))!.GetParameters().Single();

        var error = await Catch.Exception(async () => await GeneratedValueResolution.Resolve(typeof(GeneratesId), parameter, context, scope.ServiceProvider));

        if (failCleanup)
        {
            error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        }
        else if (reject)
        {
            error.ShouldBeOfExactType<GeneratedValueRejected>();
            ((GeneratedValueRejected)error).Failures.Single().ShouldEqual("generated value denied");
        }
        else
        {
            error.ShouldBeNull();
        }
        context.GeneratedValues.Count.ShouldEqual(reject || failCleanup ? 0 : 1);
        callbacks.ShouldEqual(1);
        _events.ShouldEqual(["fresh constructed", "validator constructed", "validator disposed", "fresh disposed"]);
        ProtectedValidatorActivationFrame.Current.ShouldBeNull();
    }

    public record Id(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<Id>
    {
        public static Id New() => new(GenerateValue.Uuid());
    }

    public class GeneratesId
    {
        public void Handle([GeneratedValue] Id id) { }
    }

    public class IdValidator : ConceptValidator<Id>, IDisposable
    {
        readonly Fresh _fresh;

        public IdValidator(Fresh fresh)
        {
            _fresh = fresh;
            fresh.Reader.Events.Add("validator constructed");
        }

        public bool FailCleanup { get; set; }

        public void Dispose()
        {
            _fresh.Reader.Events.Add("validator disposed");
            if (FailCleanup) throw new InvalidOperationException("cleanup failed");
        }
    }
}
