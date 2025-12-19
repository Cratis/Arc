// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';

/* eslint-disable @typescript-eslint/no-explicit-any */
export function createFetchHelper() {
    const originalFetch = (globalThis as any).fetch;
    let currentStub: sinon.SinonStub | null = null;

    function stubFetch() {
        currentStub = sinon.stub();
        (globalThis as any).fetch = currentStub as unknown as typeof fetch;
        // Provide a `restore` on the stub so existing tests calling `stub.restore()` still work.
        (currentStub as any).restore = () => restore();
        return currentStub as sinon.SinonStub;
    }

    function restore() {
        try {
            (globalThis as any).fetch = originalFetch;
        } finally {
            currentStub = null;
        }
    }

    return { stubFetch, restore } as const;
}

export default createFetchHelper;
