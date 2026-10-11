// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Testing.for_QueryScenario;

public sealed class ObservableQueryScope(ObservableScenarioState state) : IDisposable
{
    public bool Disposed { get; private set; }
    public bool HadObserversAtDisposal { get; private set; }

    public void Dispose()
    {
        HadObserversAtDisposal = ((BehaviorSubject<ObservableScenarioReadModel>)state.Subject).HasObservers;
        Disposed = true;
    }
}
