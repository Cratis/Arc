// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries;

/// <summary>
/// Abstract base class for <see cref="IClientObservable"/> implementations, providing common async enumeration
/// and connection dispatch via the template method <see cref="HandleConnectionCore"/>.
/// </summary>
/// <typeparam name="T">Type of data being observed.</typeparam>
/// <param name="subject">The <see cref="ISubject{T}"/> the observable wraps.</param>
public abstract class ClientObservableBase<T>(ISubject<T> subject) : IClientObservable, IAsyncEnumerable<T>
{
    /// <summary>
    /// Gets the subject the observable wraps.
    /// </summary>
    protected ISubject<T> Subject { get; } = subject;

    /// <inheritdoc/>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new ObservableAsyncEnumerator<T>(Subject, cancellationToken);

    /// <inheritdoc/>
    public object GetAsynchronousEnumerator(CancellationToken cancellationToken = default) =>
        GetAsyncEnumerator(cancellationToken);

    /// <inheritdoc/>
    public async Task HandleConnection(IHttpRequestContext context) =>
        await HandleConnectionCore(context);

    /// <summary>
    /// Transport-specific connection handling implemented by each derived class.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> for this connection.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected abstract Task HandleConnectionCore(IHttpRequestContext context);
}
