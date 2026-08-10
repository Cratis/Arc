// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryEmissionGuards.given;

public class all_dependencies : Specification
{
    protected ITypes _types;
    protected ILogger<ObservableQueryEmissionGuards> _logger;
    protected FirstGuard _first;
    protected SecondGuard _second;
    protected ClaimsPrincipal _principal;
    protected ObservableQueryEmissionContext _context;
    protected ObservableQueryEmissionGuards _guards;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _logger = Substitute.For<ILogger<ObservableQueryEmissionGuards>>();
        _first = new FirstGuard();
        _second = new SecondGuard();
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "the-caller")], "test"));

        // Registered as instances so the system under test resolves the very objects these specs configured,
        // the same way it resolves an application's guard from the per-subscription scope.
        var services = new ServiceCollection();
        services.AddSingleton(_first);
        services.AddSingleton(_second);

        _context = new ObservableQueryEmissionContext(
            "MyApp.Queries.GuardedQuery",
            new QueryArguments { ["id"] = 42 },
            _principal,
            CorrelationId.New(),
            services.BuildServiceProvider(),
            true,
            CancellationToken.None);
    }

    /// <summary>
    /// Builds the system under test over the given guard types, in the order they are discovered.
    /// </summary>
    /// <param name="guardTypes">The guard types discovery yields.</param>
    protected void DiscoverGuards(params Type[] guardTypes)
    {
        _types.FindMultiple<IGuardObservableQueryEmission>().Returns(guardTypes);
        _guards = new ObservableQueryEmissionGuards(_types, _logger);
    }

    public class FirstGuard : RecordingGuard;

    public class SecondGuard : RecordingGuard;

    public abstract class RecordingGuard : IGuardObservableQueryEmission
    {
        public List<ObservableQueryEmissionContext> Calls { get; } = [];

        public ObservableQueryEmissionVerdict Verdict { get; set; } = ObservableQueryEmissionVerdict.Allow;

        public Exception Failure { get; set; }

        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            Calls.Add(context);

            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(Verdict);
        }
    }
}
