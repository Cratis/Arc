// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;

namespace Cratis.Arc.Availability.given;

public class a_suspension_host : an_availability_host
{
    protected static readonly Type Ungated = typeof(UngatedCommand);
    protected static readonly Type Invoice = typeof(Fleet.Billing.InvoiceCommand);
    protected static readonly Type Scores = typeof(Fleet.Quality.ScoresReadModel);

    /// <summary>
    /// Gets the path of the feature both the Billing and the Quality slices belong to.
    /// </summary>
    protected static string FeaturePath
    {
        get
        {
            var location = SliceLocation.For(Invoice);
            return $"{location.Module.Value}/{location.Feature.Value}";
        }
    }

    protected ValueTask<AvailabilityDecision> Decide(Type part) => _gate.Decide(part, _scope.ServiceProvider, CancellationToken.None);
}
