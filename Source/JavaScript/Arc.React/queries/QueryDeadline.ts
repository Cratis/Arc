// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Bounds one hook execution, including time spent adopting a superseding request. */
export class QueryDeadline {
    private readonly expired?: Promise<never>;
    private timer?: ReturnType<typeof setTimeout>;

    constructor(timeoutMs?: number, abortController?: AbortController) {
        if (timeoutMs !== undefined && timeoutMs > 0 && Number.isFinite(timeoutMs)) {
            this.expired = new Promise<never>((_, reject) => {
                this.timer = setTimeout(() => {
                    // Reject first so an abort-aware transport cannot replace the timeout with AbortError.
                    reject(new Error(`Query timed out after ${timeoutMs} ms.`));
                    abortController?.abort();
                }, timeoutMs);
            });
        }
    }

    waitFor<T>(request: Promise<T>): Promise<T> {
        return this.expired ? Promise.race([request, this.expired]) : request;
    }

    dispose(): void {
        clearTimeout(this.timer);
    }
}
