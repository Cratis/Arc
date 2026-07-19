// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for making every command a transactional scope.
/// </summary>
public static class CommandTransactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IEventLog"/> that enrolls a command's appends in its unit of work, making every
    /// command a transactional scope together with <see cref="TransactionalCommandScope"/> (which is discovered
    /// automatically): the events a command appends commit atomically when the command succeeds and roll back when it
    /// does not. To append outside the command's transaction, use <c>IEventStore.EventLog</c> directly, which appends
    /// immediately.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCommandTransactions(this IServiceCollection services)
    {
        services.AddScoped<IEventLog>(serviceProvider =>
            new TransactionalEventLog(serviceProvider.GetRequiredService<IEventStore>().EventLog));

        return services;
    }
}
