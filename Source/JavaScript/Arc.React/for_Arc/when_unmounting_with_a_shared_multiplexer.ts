// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
import { vi } from 'vitest';
import { getOrCreateMultiplexer, getSharedMultiplexer, resetSharedMultiplexer } from '@cratis/arc/queries';
import { Arc } from '../Arc.js';

describe('when unmounting Arc with a shared multiplexer', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ id: '', name: '', roles: [], details: {} }))));
    });

    afterEach(() => {
        resetSharedMultiplexer();
        vi.unstubAllGlobals();
    });

    it('keeps the multiplexer through StrictMode replay but closes it on unmount', async () => {
        const dispose = vi.fn();
        const multiplexer = getOrCreateMultiplexer(() => ({
            dispose, subscribe: vi.fn(), unsubscribe: vi.fn(), queryCount: 0,
            isConnected: true, lastPingLatency: 0, averageLatency: 0
        }), 'test', 1);

        const view = render(React.createElement(React.StrictMode, null, React.createElement(Arc)));
        await act(async () => { await new Promise(resolve => setTimeout(resolve, 0)); });
        (getSharedMultiplexer() === multiplexer).should.be.true;
        dispose.mock.calls.length.should.equal(0);

        view.unmount();
        (getSharedMultiplexer() === undefined).should.be.true;
        dispose.mock.calls.length.should.equal(1);
    });
});
