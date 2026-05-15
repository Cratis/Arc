// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Provides an interceptor that decrypts Chronicle compliance (PII) properties on a read model before it is served to a client.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to intercept.</typeparam>
/// <param name="readModels">The <see cref="IReadModels"/> used to release (decrypt) encrypted PII properties.</param>
/// <param name="logger">The <see cref="ILogger"/> used for diagnostics.</param>
public class ReadModelInterceptor<TReadModel>(IReadModels readModels, ILogger<ReadModelInterceptor<TReadModel>> logger) : IInterceptReadModel<TReadModel>
{
    /// <inheritdoc/>
    public async Task<TReadModel> Intercept(TReadModel readModel)
    {
        logger.InterceptingReadModel(typeof(TReadModel).FullName ?? typeof(TReadModel).Name);

        var releasedReadModel = await readModels.Release(readModel);

        logger.InterceptedReadModel(typeof(TReadModel).FullName ?? typeof(TReadModel).Name);

        return releasedReadModel;
    }
}
