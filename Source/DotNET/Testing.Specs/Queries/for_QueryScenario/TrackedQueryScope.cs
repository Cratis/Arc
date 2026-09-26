// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.for_QueryScenario;

public class TrackedQueryScope : IDisposable
{
    public bool Disposed { get; private set; }
    public void Dispose() => Disposed = true;
}
