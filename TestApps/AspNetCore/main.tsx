// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Arc } from '@cratis/arc.react';
import { App, Page } from './App';
import { QueryTransportMethod } from '../../Source/JavaScript/Arc/queries/QueryTransportMethod';

const Root = () => {
    const [transport, setTransport] = useState<QueryTransportMethod>(QueryTransportMethod.WebSocket);
    const [connectionCount, setConnectionCount] = useState(1);
    const [directMode, setDirectMode] = useState(false);
    const [configKey, setConfigKey] = useState(0);
    const [page, setPage] = useState<Page>('ticker');

    const apply = (newTransport: QueryTransportMethod, newCount: number, newDirect: boolean) => {
        setTransport(newTransport);
        setConnectionCount(newCount);
        setDirectMode(newDirect);
        setConfigKey(k => k + 1);
    };

    return (
        <StrictMode>
            <div style={{ padding: '8px 16px', background: '#f0f0f0', borderBottom: '1px solid #ccc', display: 'flex', gap: 16, alignItems: 'center', fontSize: 14 }}>
                <label>
                    Transport{' '}
                    <select value={transport} onChange={e => apply(e.target.value as QueryTransportMethod, connectionCount, directMode)}>
                        <option value={QueryTransportMethod.WebSocket}>WebSocket</option>
                        <option value={QueryTransportMethod.ServerSentEvents}>Server-Sent Events</option>
                    </select>
                </label>
                <label style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    Connections{' '}
                    <button onClick={() => apply(transport, Math.max(1, connectionCount - 1), directMode)} style={{ width: 24, cursor: 'pointer' }}>-</button>
                    <span style={{ minWidth: 20, textAlign: 'center' }}>{connectionCount}</span>
                    <button onClick={() => apply(transport, Math.min(10, connectionCount + 1), directMode)} style={{ width: 24, cursor: 'pointer' }}>+</button>
                </label>
                <label>
                    <input type="checkbox" checked={directMode} onChange={e => apply(transport, connectionCount, e.target.checked)} />{' '}
                    Direct mode
                </label>
            </div>
            <Arc key={configKey} queryTransportMethod={transport} queryConnectionCount={connectionCount} queryDirectMode={directMode}>
                <App page={page} onPageChange={setPage} />
            </Arc>
        </StrictMode>
    );
};

createRoot(document.getElementById('root')!).render(<Root />);
