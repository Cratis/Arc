// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Observe } from 'Features/AspNetCore/Observe';

/**
 * Page that displays a live-updating counter streaming from the server via SSE.
 */
export const TickerPage = () => {
    const [result] = Observe.use();

    return (
        <div>
            <h2>Ticker</h2>
            <p>
                Subscribes to <code>Ticker.Observe()</code> via SSE. The counter increments
                every second on the server and is pushed in real time.
            </p>
            {result.isPerforming && <p>Connecting…</p>}
            {result.hasData && (
                <div style={{ fontSize: 48, fontWeight: 'bold', textAlign: 'center', padding: 32 }}>
                    {result.data.count}
                </div>
            )}
            {result.hasData && (
                <p style={{ textAlign: 'center', color: '#888', fontSize: 12 }}>
                    Last updated: {new Date(result.data.lastUpdated).toISOString()}
                </p>
            )}
        </div>
    );
};
