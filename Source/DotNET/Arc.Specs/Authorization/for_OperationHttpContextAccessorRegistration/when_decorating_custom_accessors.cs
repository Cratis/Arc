// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_OperationHttpContextAccessorRegistration;

public class when_decorating_custom_accessors
{
    [Fact]
    public void should_dispose_container_created_type()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, DisposableAccessor>();
        OperationHttpContextAccessorRegistration.Add(services);
        var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        accessor.ShouldBeOfExactType<OperationHttpContextAccessor>();
        provider.Dispose();
        DisposableAccessor.Last!.Disposed.ShouldBeTrue();
    }

    [Fact]
    public void should_dispose_container_created_factory()
    {
        var inner = new DisposableAccessor();
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(_ => inner);
        OperationHttpContextAccessorRegistration.Add(services);
        using (var provider = services.BuildServiceProvider())
        {
            _ = provider.GetRequiredService<IHttpContextAccessor>();
        }
        inner.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_dispose_async_factory_and_not_external_instance()
    {
        var inner = new AsyncDisposableAccessor();
        var external = new DisposableAccessor();
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(external);
        services.AddSingleton<IHttpContextAccessor>(_ => inner);
        OperationHttpContextAccessorRegistration.Add(services);
        await using (var provider = services.BuildServiceProvider())
        {
            _ = provider.GetRequiredService<IHttpContextAccessor>();
        }
        inner.Disposed.ShouldBeTrue();
        external.Disposed.ShouldBeFalse();
    }

    [Fact]
    public void should_not_dispose_an_external_original_instance()
    {
        var external = new DisposableAccessor();
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(external);
        OperationHttpContextAccessorRegistration.Add(services);
        using (var provider = services.BuildServiceProvider())
        {
            _ = provider.GetRequiredService<IHttpContextAccessor>();
        }
        external.Disposed.ShouldBeFalse();
    }

    [Fact]
    public void should_ignore_keyed_registration_when_selecting_original()
    {
        var original = new DisposableAccessor();
        var keyed = new DisposableAccessor();
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(original);
        services.AddKeyedSingleton<IHttpContextAccessor>("custom", keyed);
        OperationHttpContextAccessorRegistration.Add(services);
        using (var provider = services.BuildServiceProvider())
        {
            provider.GetRequiredService<IHttpContextAccessor>().ShouldBeOfExactType<OperationHttpContextAccessor>();
            provider.GetRequiredKeyedService<IHttpContextAccessor>("custom").ShouldEqual(keyed);
        }
        original.Disposed.ShouldBeFalse();
        keyed.Disposed.ShouldBeFalse();
    }

    sealed class DisposableAccessor : IHttpContextAccessor, IDisposable
    {
        internal static DisposableAccessor? Last;
        public DisposableAccessor() => Last = this;
        public HttpContext? HttpContext { get; set; }
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    sealed class AsyncDisposableAccessor : IHttpContextAccessor, IAsyncDisposable
    {
        public HttpContext? HttpContext { get; set; }
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
