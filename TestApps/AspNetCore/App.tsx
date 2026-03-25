// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TickerPage } from './Features/Ticker/TickerPage';
import { LiveFeedPage } from './Features/LiveFeed/LiveFeedPage';
import { QueryShowcasePage } from './Features/QueryShowcase/QueryShowcasePage';

export type Page = 'ticker' | 'livefeed' | 'queryshowcase';

interface AppProps {
    page: Page;
    onPageChange: (page: Page) => void;
}

/**
 * Root application component with simple tab navigation.
 */
export const App = ({ page, onPageChange: setPage }: AppProps) => {

    return (
        <div style={{ fontFamily: 'sans-serif', maxWidth: 1000, margin: '0 auto', padding: 24 }}>
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
                <button
                    onClick={() => setPage('queryshowcase')}
                    style={{ fontWeight: page === 'queryshowcase' ? 'bold' : 'normal' }}
                >
                    Query Showcase
                </button>
            </nav>
            {page === 'ticker' && <TickerPage />}
            {page === 'livefeed' && <LiveFeedPage />}
            {page === 'queryshowcase' && <QueryShowcasePage />}
        </div>
    );
};
