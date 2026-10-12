// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Tenancy;
using Cratis.Concepts;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.given;

public class a_context_generated_value_resolver : a_generated_value_resolver
{
    protected readonly DateTimeOffset _now = new(2024, 6, 15, 12, 34, 56, TimeSpan.Zero);
    protected ITenantIdAccessor _tenant;

    void Establish()
    {
        For<CreatesContextId>();
        _context = _context with { ReceivedAt = _now };
        _tenant = Substitute.For<ITenantIdAccessor>();
        _tenant.Current.Returns(new TenantId("tenant-one"));
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(_tenant);
    }

    public sealed record ContextId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<ContextId>
    {
        static ContextId IGeneratableInContext<ContextId>.New(GenerationContext context) => new($"{context.Now:O}|{context.Tenant}");
    }

    public class CreatesContextId
    {
        public void Handle([GeneratedValue] ContextId id) { }
    }

    public record DualId(string Value) : ConceptAs<string>(Value), IGeneratable<DualId>, IGeneratableInContext<DualId>
    {
        public static DualId New() => new("plain");
        public static DualId New(GenerationContext context) => new($"{context.Now:O}|{context.Tenant}");
    }

    public class CreatesDualId
    {
        public void Handle([GeneratedValue] DualId id) { }
    }
}
