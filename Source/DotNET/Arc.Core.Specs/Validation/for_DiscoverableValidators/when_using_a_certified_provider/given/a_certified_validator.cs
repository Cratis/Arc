// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.for_CommandDecisionPolicy;
using Cratis.Arc.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider.given;

public class a_certified_validator : Specification
{
    protected IServiceCollection _services;
    protected ITypes _types;
    protected DiscoverableValidators _validators;
    protected readonly List<string> _events = [];
    protected IServiceProvider _root;
    protected IServiceScope _scope;
    protected IDisposable _policy;
    private protected ProtectedValidatorActivationFrame _frame;

    void Establish()
    {
        _services = new ServiceCollection();
        _types = Substitute.For<ITypes>();
        _types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ReadingValidator)]);
        _validators = new(_types);
        var origins = ProtectedDecisionRegistrationOrigins.For(_services);
        origins.DiscoverValidators([typeof(ReadingValidator), typeof(EmptyValidator), typeof(LateOpaqueValidator)]);
        var reader = ServiceDescriptor.Scoped<IReader>(_ => new Reader(_events));
        _services.Add(reader);
        origins.RecognizeReaders(typeof(IReader), []);
        origins.Record(reader, new(VerifyRead: value => value.ShouldBeOfExactType<Reader>(), IsCommandReader: true));
        AddConvention(typeof(ReadingValidator));
        _services.AddTransient<Fresh>();
    }

    protected void AddConvention(Type type)
    {
        var descriptor = ServiceDescriptor.Transient(type, type);
        _services.Add(descriptor);
        ProtectedDecisionRegistrationOrigins.For(_services).Record(descriptor, new(IsConvention: true));
    }

    protected void Build()
    {
        _root = _services.BuildProtectedDecisionServiceProvider();
        _scope = _root.CreateScope();
        _policy = CommandDecisionPolicy.Begin(typeof(Model));
        _frame = new();
    }

    async Task Destroy()
    {
        if (_frame is not null) await _frame.DisposeAsync();
        _policy?.Dispose();
        _scope?.Dispose();
        if (_root is IAsyncDisposable asynchronous) await asynchronous.DisposeAsync();
    }

    [ProtectedForSpecs]
    public record Model(string Name = "valid");
    public interface IReader
    {
        List<string> Events { get; }
    }
    public sealed record Reader(List<string> Events) : IReader;
    public class Fresh(IReader reader) : IDisposable
    {
        public IReader Reader { get; } = Record(reader);
        public void Dispose() => Reader.Events.Add("fresh disposed");
        static IReader Record(IReader reader)
        {
            reader.Events.Add("fresh constructed");
            return reader;
        }
    }
    public class ReadingValidator : CommandValidator<Model>, IDisposable
    {
        public ReadingValidator(Fresh fresh)
        {
            Fresh = fresh;
            fresh.Reader.Events.Add("validator constructed");
            RuleFor(model => model.Name).NotEmpty();
        }
        public Fresh Fresh { get; }
        public void Dispose() => Fresh.Reader.Events.Add("validator disposed");
    }
    public class EmptyValidator : AbstractValidator<Model>;
    public class Opaque;
    public class LateOpaqueValidator(Fresh fresh, Opaque opaque) : AbstractValidator<Model>
    {
        public Fresh Fresh { get; } = fresh;
        public Opaque Opaque { get; } = opaque;
    }
}
