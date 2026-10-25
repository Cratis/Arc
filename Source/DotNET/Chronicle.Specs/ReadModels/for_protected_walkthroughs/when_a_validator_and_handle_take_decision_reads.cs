// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

public class when_a_validator_and_handle_take_decision_reads : given.a_protected_read
{
    [Fact]
    public async Task should_share_one_fold_between_constructor_and_handle_parameters()
    {
        await Seed();
        var command = new BorrowWithDecision(_book);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(BorrowWithDecision));
        Begin(command);
        try
        {
            var artifacts = Substitute.For<IClientArtifactsProvider>();
            artifacts.ModelBoundProjections.Returns([typeof(BookStock)]);
            var services = new ServiceCollection().AddReadModels(artifacts);
            services.AddSingleton(_services.GetRequiredService<ICommandContextAccessor>());
            services.AddSingleton(_services.GetRequiredService<IDecisionReads>());
            await using var provider = services.BuildServiceProvider();
            var types = Substitute.For<Cratis.Types.ITypes>();
            types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(BorrowWithDecisionValidator)]);
            new DiscoverableValidators(types).TryGet(typeof(BorrowWithDecision), provider, out var validator).ShouldBeTrue();
            (await validator!.ValidateAsync(new FluentValidation.ValidationContext<BorrowWithDecision>(command), global::Xunit.TestContext.Current.CancellationToken)).IsValid.ShouldBeTrue();
            var parameter = typeof(BorrowWithDecision).GetMethod(nameof(BorrowWithDecision.Handle))!.GetParameters()[0];
            var handle = (DecisionRead<BookStock>)ParameterDependencyResolver.Resolve(provider, parameter, _ => new UnableToResolveReadModelFromCommandContext(typeof(BookStock)))!;
            handle.ShouldBeSame(((BorrowWithDecisionValidator)validator).Stock);
            command.Handle(handle).Available.ShouldEqual(1);
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record BorrowWithDecision(BookId Id)
    {
        public BookStock Handle(DecisionRead<BookStock> stock) => stock.Instance!;
    }

    public class BorrowWithDecisionValidator : CommandValidator<BorrowWithDecision>
    {
        public BorrowWithDecisionValidator(DecisionRead<BookStock> stock)
        {
            Stock = stock;
            RuleFor(command => command.Id).Must(_ => stock.Instance is { Available: > 0 });
        }

        public DecisionRead<BookStock> Stock { get; }
    }
}
