// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Concepts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider;

public class with_nested_models_and_concepts : given.a_certified_validator
{
    IReader _childReader;
    IReader _conceptReader;
    ValidationResult[] _results;

    void Establish()
    {
        _types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ReadingValidator), typeof(LabelValidator)]);
        _validators = new(_types);
        _services.AddProtectedDecisionValidator<ReadingValidator>(validator => _childReader = validator.Fresh.Reader);
        _services.AddProtectedDecisionValidator<LabelValidator>(validator => _conceptReader = validator.Reader);
    }

    async Task Because()
    {
        Build();
        var graph = new ModelGraphValidator(_validators, new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance));
        _results = (await graph.Validate(new(new Parent(new Model(), new Label("")), _scope.ServiceProvider))).ToArray();
    }

    [Fact] void should_use_the_same_executing_scope_for_child_and_concept_validators() => _childReader.ShouldBeSame(_conceptReader);
    [Fact] void should_preserve_the_nested_concept_denial() => _results.Single().Message.ShouldEqual("label required");
    [Fact] void should_dispose_nested_owned_dependencies_at_the_end_of_the_graph() => _events.ShouldContain("fresh disposed");

    public record Parent(Model Child, Label Label);
    public record Label(string Value) : ConceptAs<string>(Value);
    public class LabelValidator : AbstractValidator<Label>, IDiscoverableValidator<Label>
    {
        public LabelValidator(IReader reader)
        {
            Reader = reader;
            RuleFor(value => value.Value).NotEmpty().WithMessage("label required");
        }
        public IReader Reader { get; }
    }
}
