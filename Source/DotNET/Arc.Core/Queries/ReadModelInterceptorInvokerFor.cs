// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Invokes <see cref="IInterceptReadModel{TReadModel}"/> interceptors for a specific read model type.
/// </summary>
/// <typeparam name="TReadModel">Type of read model.</typeparam>
internal sealed class ReadModelInterceptorInvokerFor<TReadModel> : ReadModelInterceptorInvoker
{
    /// <inheritdoc/>
    public override Type InterceptorInterface => typeof(IInterceptReadModel<TReadModel>);

    /// <inheritdoc/>
    public override IReadOnlyList<object> GetServiceInterceptors(IServiceProvider serviceProvider) =>
        serviceProvider.GetService(typeof(IEnumerable<IInterceptReadModel<TReadModel>>)) is IEnumerable<IInterceptReadModel<TReadModel>> interceptors
            ? [.. interceptors]
            : [];

    /// <inheritdoc/>
    public override async Task<object> Intercept(object interceptor, object readModel)
    {
        var typedReadModel = readModel switch
        {
            null => default!,
            TReadModel typed => typed,
            _ => throw new ReadModelIsNotOfExpectedType(readModel.GetType(), typeof(TReadModel))
        };

        return (await ((IInterceptReadModel<TReadModel>)interceptor).Intercept(typedReadModel))!;
    }
}
