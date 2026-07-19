// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for making every command a transactional scope.
/// </summary>
public static class CommandTransactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the decorators that make every command a transactional scope — appends enroll in the command's unit of
    /// work and are committed atomically when the command succeeds, or rolled back when it does not. To append outside
    /// the command's transaction, use <c>IEventStore.EventLog</c> directly, which appends immediately.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCommandTransactions(this IServiceCollection services)
    {
        services.AddScoped<IEventLog>(serviceProvider =>
            new TransactionalEventLog(
                serviceProvider.GetRequiredService<IEventStore>().EventLog,
                serviceProvider.GetRequiredService<IUnitOfWorkManager>()));

        services.AddSingleton<TransactionalCommandPipeline>();
        services.AddSingleton<ICommandPipeline>(serviceProvider => serviceProvider.GetRequiredService<TransactionalCommandPipeline>());
        services.AddSingleton<ICommandPipelineWithCancellation>(serviceProvider => serviceProvider.GetRequiredService<TransactionalCommandPipeline>());

        return services;
    }
}
