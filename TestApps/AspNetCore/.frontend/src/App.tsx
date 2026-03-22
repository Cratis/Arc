// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { TickerPage } from './TickerPage';
import { LiveFeedPage } from './LiveFeedPage';

type Page = 'ticker' | 'livefeed';

/**
 * Root application component with simple tab navigation.
 */
export const App = () => {
    const [page, setPage] = useState<Page>('ticker');

    return (
        <div style={{ fontFamily: 'sans-serif', maxWidth: 800, margin: '0 auto', padding: 24 }}>
            <h1>AspNetCore TestApp</h1>
            <nav style={{ display: 'flex', gap: 12, marginBottom: 24 }}>
                <button
                    onClick={() => setPage('ticker')}
                    style={{ fontWeight: page === 'ticker' ? 'bold' : 'normal' }}
                >
                    Ticker
                </button>
                <button
                    onClick={() => setPage('livefeed')}
                    style={{ fontWeight: page === 'livefeed' ? 'bold' : 'normal' }}
                >
                    Live Feed
                </button>
            </nav>
            {page === 'ticker' && <TickerPage />}
            {page === 'livefeed' && <LiveFeedPage />}
        </div>
    );
};
