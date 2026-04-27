// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TickerPage } from './Features/Ticker/TickerPage';
import { LiveFeedPage } from './Features/LiveFeed/LiveFeedPage';
import { QueryShowcasePage } from './Features/QueryShowcase/QueryShowcasePage';
import { ConditionalQueriesPage } from './Features/ConditionalQueries/ConditionalQueriesPage';
import { AuthenticationQueriesPage } from './Features/AuthenticationQueries/AuthenticationQueriesPage';
import { ChangeStreamPage } from './Features/ChangeStream/ChangeStreamPage';
import { CrossCuttingAuthorizationPage } from './Features/CrossCuttingAuthorization/CrossCuttingAuthorizationPage';

export type Page = 'authenticationqueries' | 'crosscuttingauthorization' | 'ticker' | 'livefeed' | 'queryshowcase' | 'conditionalqueries' | 'changestream';

interface AppProps {
    title: string;
    page: Page;
    onPageChange: (page: Page) => void;
}

/**
 * Root application component with simple tab navigation.
 */
export const App = ({ title, page, onPageChange: setPage }: AppProps) => {
    return (
        <div style={{ fontFamily: 'sans-serif', maxWidth: 1000, margin: '0 auto', padding: 24 }}>
            <h1>{title}</h1>
            <nav style={{ display: 'flex', gap: 12, marginBottom: 24, flexWrap: 'wrap' }}>
                <button
                    onClick={() => setPage('authenticationqueries')}
                    style={{ fontWeight: page === 'authenticationqueries' ? 'bold' : 'normal' }}
                >
                    Authentication Queries
                </button>
                <button
                    onClick={() => setPage('crosscuttingauthorization')}
                    style={{ fontWeight: page === 'crosscuttingauthorization' ? 'bold' : 'normal' }}
                >
                    Cross-Cutting Authorization
                </button>
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
                <button
                    onClick={() => setPage('conditionalqueries')}
                    style={{ fontWeight: page === 'conditionalqueries' ? 'bold' : 'normal' }}
                >
                    Conditional Queries
                </button>
                <button
                    onClick={() => setPage('changestream')}
                    style={{ fontWeight: page === 'changestream' ? 'bold' : 'normal' }}
                >
                    Change Stream
                </button>
            </nav>
            {page === 'authenticationqueries' && <AuthenticationQueriesPage />}
            {page === 'crosscuttingauthorization' && <CrossCuttingAuthorizationPage />}
            {page === 'ticker' && <TickerPage />}
            {page === 'livefeed' && <LiveFeedPage />}
            {page === 'queryshowcase' && <QueryShowcasePage />}
            {page === 'conditionalqueries' && <ConditionalQueriesPage />}
            {page === 'changestream' && <ChangeStreamPage />}
        </div>
    );
};
