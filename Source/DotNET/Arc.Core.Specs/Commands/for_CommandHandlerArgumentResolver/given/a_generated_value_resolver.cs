// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.given;

public class a_generated_value_resolver : a_command_handler_argument_resolver
{
    protected CommandHandlerArgumentResolution _resolution;

    void Establish() => For<CreatesId>();

    protected void For<T>()
        where T : new()
    {
        _handler = new ModelBoundCommandHandler(typeof(T), typeof(T).GetMethod("Handle")!);
        _context = new(_correlationId, typeof(T), new T(), [], new());
    }

    protected void WithValidator(IValidator validator)
    {
        var validators = Substitute.For<IDiscoverableValidators>();
        validators.TryGet(typeof(Id), _serviceProvider, out Arg.Any<IValidator>()).Returns(call =>
        {
            call[2] = validator;
            return true;
        });
        _serviceProvider.GetService(typeof(IDiscoverableValidators)).Returns(validators);
    }

    public record Id(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<Id>
    {
        public static Id New() => new(GenerateValue.Uuid());
    }

    public class CreatesId
    {
        public void Handle([GeneratedValue] Id id) { }
    }

    public class CreatesNamedId
    {
        public void Handle([GeneratedValue("cart")] Id id) { }
    }

    public class CreatesTwoIds
    {
        public void Handle([GeneratedValue] Id first, [GeneratedValue] Id second) { }
    }
}
