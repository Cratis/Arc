// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Arc } from '@cratis/arc.react';
import { App } from './App';
import { QueryTransportMethod } from '../../Source/JavaScript/Arc/queries/QueryTransportMethod';

const Root = () => {
    const [transport, setTransport] = useState<QueryTransportMethod>(QueryTransportMethod.WebSocket);
    const [connectionCount, setConnectionCount] = useState(1);
    const [directMode, setDirectMode] = useState(false);

    return (
        <StrictMode>
            <div style={{ padding: '8px 16px', background: '#f0f0f0', borderBottom: '1px solid #ccc', display: 'flex', gap: 16, alignItems: 'center', fontSize: 14 }}>
                <label>
                    Transport{' '}
                    <select value={transport} onChange={e => setTransport(e.target.value as QueryTransportMethod)}>
                        <option value={QueryTransportMethod.WebSocket}>WebSocket</option>
                        <option value={QueryTransportMethod.ServerSentEvents}>Server-Sent Events</option>
                    </select>
                </label>
                <label>
                    Connections{' '}
                    <input type="number" min={1} max={10} value={connectionCount} onChange={e => setConnectionCount(Math.max(1, Number(e.target.value)))} style={{ width: 50 }} />
                </label>
                <label>
                    <input type="checkbox" checked={directMode} onChange={e => setDirectMode(e.target.checked)} />{' '}
                    Direct mode
                </label>
            </div>
            <Arc queryTransportMethod={transport} queryConnectionCount={connectionCount} queryDirectMode={directMode}>
                <App />
            </Arc>
        </StrictMode>
    );
};

createRoot(document.getElementById('root')!).render(<Root />);
