// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Concepts;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

public class a_parameter_lookup : Specification
{
    protected IReadModels _readModels;
    protected ServiceProvider _services;
    protected CommandContext _context;
    protected IReadOnlyList<object> _provided;
    protected CommandHandlerArgumentResolution _arguments;
    protected readonly Balance _from = new(new("from"));
    protected readonly Balance _to = new(new("to"));
    protected readonly Balance _subject = new(new("subject"));

    void Establish()
    {
        _readModels = Substitute.For<IReadModels>();
        foreach (var balance in new[] { _from, _to, _subject })
        {
            _readModels.GetInstanceById(typeof(Balance), (ReadModelKey)balance.Id.Value, default).Returns(Task.FromResult<object>(balance));
            _readModels.Release(balance).Returns(balance);
        }
    }

    protected void Configure(object command)
    {
        var accessor = Substitute.For<ICommandContextAccessor>();
        _context = new(CorrelationId.New(), command.GetType(), command, [], new()
        {
            [WellKnownCommandContextKeys.EventSourceId] = (EventSourceId)"subject"
        });
        accessor.Current.Returns(_ => _context);
        _services = new ServiceCollection()
            .AddSingleton(accessor)
            .AddSingleton(_readModels)
            .AddTransient(_ => _subject)
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>(
                [new ChronicleParameterDependencyResolver([typeof(Balance)])]))
            .BuildServiceProvider();
        _context = _context with { ServiceProvider = _services };
    }

    protected async Task ResolveHandle()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.Parameters.Returns(_context.Type.GetMethod("Handle")!.GetParameters());
        _arguments = await new CommandHandlerArgumentResolver(new CommandProvideInvoker()).Resolve(handler, _context, _services, null);
    }

    protected async Task ResolveProvide() => _provided = await new CommandProvideInvoker().Invoke(_context, _services);

    void Destroy() => _services?.Dispose();

    public record AccountId(string Value) : ConceptAs<string>(Value);
    public record Balance(AccountId Id);

    public record Transfer(AccountId From, AccountId To)
    {
        public void Handle([LookUpBy(nameof(From))] Balance from, [LookUpBy(nameof(To))] Balance to) { }
        public (Balance, Balance) Provide([LookUpBy(nameof(From))] Balance from, [LookUpBy(nameof(To))] Balance to) => (from, to);
    }

    public record Convention(AccountId Account)
    {
        public Balance Provide(Balance balance) => balance;
    }

    public class TransferValidator([LookUpBy(nameof(Transfer.From))] Balance from, [LookUpBy(nameof(Transfer.To))] Balance to) : CommandValidator<Transfer>
    {
        public Balance From { get; } = from;
        public Balance To { get; } = to;
    }
}
