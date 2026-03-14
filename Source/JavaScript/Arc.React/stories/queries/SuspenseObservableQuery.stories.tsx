// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* eslint-disable @typescript-eslint/no-explicit-any */
import React, { useState } from 'react';
import { Meta } from '@storybook/react';
import { ObservableQueryFor, QueryResult, ObservableQuerySubscription } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import {
    useSuspenseObservableQuery,
    clearSuspenseObservableQueryCache,
    QueryFailed,
    QueryErrorBoundary,
} from '../../queries';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { StoryContainer, StorySection, StoryBadge } from '../StoryContainer';

// ---------------------------------------------------------------------------
// Data model
// ---------------------------------------------------------------------------

interface LogEntry {
    id: string;
    timestamp: string;
    level: 'info' | 'warn' | 'error';
    message: string;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let _logCounter = 0;

function makeLogEntry(level: LogEntry['level'], message: string): LogEntry {
    _logCounter += 1;
    return {
        id: String(_logCounter),
        timestamp: new Date().toISOString().replace('T', ' ').substring(0, 19),
        level,
        message,
    };
}

function makeQueryResult(entries: LogEntry[]): QueryResult<LogEntry[]> {
    return new QueryResult(
        {
            data: entries as unknown as object,
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: entries.length, totalItems: entries.length, totalPages: 1 },
        },
        Object,
        true
    ) as QueryResult<LogEntry[]>;
}

// ---------------------------------------------------------------------------
// Fake observable query classes
// ---------------------------------------------------------------------------

type LogCallback = (result: QueryResult<LogEntry[]>) => void;

/**
 * Simulates an observable query that:
 *  - Delivers initial data after a 1.5 s delay.
 *  - Pushes a new log entry every 2 s thereafter.
 *
 * The static `push` method lets external code (e.g. a story button) inject
 * additional entries on demand.
 */
class StreamingLogQuery extends ObservableQueryFor<LogEntry[]> {
    readonly route = '/api/stories/logs';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: LogEntry[] = [];

    constructor() {
        super(Object, true);
    }

    get requiredRequestParameters(): string[] {
        return [];
    }

    private static _callbacks: LogCallback[] = [];
    private static _entries: LogEntry[] = [];
    private static _intervalId: ReturnType<typeof setInterval> | null = null;

    static reset() {
        if (this._intervalId !== null) {
            clearInterval(this._intervalId);
            this._intervalId = null;
        }
        this._callbacks = [];
        this._entries = [];
        _logCounter = 0;
    }

    /** Inject a new entry and broadcast to all active subscriptions. */
    static push(level: LogEntry['level'], message: string) {
        const entry = makeLogEntry(level, message);
        this._entries = [...this._entries, entry];
        const result = makeQueryResult(this._entries);
        this._callbacks.forEach(cb => cb(result));
    }

    subscribe(callback: LogCallback): ObservableQuerySubscription<LogEntry[]> {
        StreamingLogQuery._callbacks.push(callback);

        // Initial data after a simulated delay.
        setTimeout(() => {
            StreamingLogQuery._entries = [
                makeLogEntry('info', 'Application started'),
                makeLogEntry('info', 'Configuration loaded'),
                makeLogEntry('warn', 'Cache miss — warming up'),
            ];
            callback(makeQueryResult(StreamingLogQuery._entries));

            // Automatic periodic updates.
            if (StreamingLogQuery._intervalId === null) {
                StreamingLogQuery._intervalId = setInterval(() => {
                    StreamingLogQuery.push('info', `Heartbeat — ${new Date().toLocaleTimeString()}`);
                }, 2000);
            }
        }, 1500);

        return {
            unsubscribe: () => {
                StreamingLogQuery._callbacks = StreamingLogQuery._callbacks.filter(cb => cb !== callback);
                if (StreamingLogQuery._callbacks.length === 0 && StreamingLogQuery._intervalId !== null) {
                    clearInterval(StreamingLogQuery._intervalId);
                    StreamingLogQuery._intervalId = null;
                }
            },
        } as unknown as ObservableQuerySubscription<LogEntry[]>;
    }
}

/** Simulates an observable that sends an exception as its first message. */
class FailingLogQuery extends ObservableQueryFor<LogEntry[]> {
    readonly route = '/api/stories/logs-error';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: LogEntry[] = [];

    constructor() {
        super(Object, true);
    }

    get requiredRequestParameters(): string[] {
        return [];
    }

    subscribe(callback: LogCallback): ObservableQuerySubscription<LogEntry[]> {
        setTimeout(() => {
            callback(
                new QueryResult(
                    {
                        data: [] as unknown as object,
                        isSuccess: false,
                        isAuthorized: true,
                        isValid: true,
                        hasExceptions: true,
                        validationResults: [],
                        exceptionMessages: [
                            'WebSocket connection to log stream dropped',
                            'Unable to resume subscription after reconnect',
                        ],
                        exceptionStackTrace:
                            'at LogStreamConnection.Connect() in LogStream.cs:88\n' +
                            'at LogQueryHandler.Subscribe() in LogQueryHandler.cs:24',
                        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
                    },
                    Object,
                    true
                ) as QueryResult<LogEntry[]>
            );
        }, 1000);

        return { unsubscribe: () => {} } as unknown as ObservableQuerySubscription<LogEntry[]>;
    }
}

// ---------------------------------------------------------------------------
// Presentational components
// ---------------------------------------------------------------------------

const LoadingSpinner = () => (
    <div
        style={{
            padding: '2rem',
            textAlign: 'center',
            color: 'var(--color-text-muted)',
        }}
    >
        ⏳ Waiting for first message…
    </div>
);

const levelBadgeVariant = (level: LogEntry['level']): 'info' | 'warning' | 'error' =>
    level === 'error' ? 'error' : level === 'warn' ? 'warning' : 'info';

const LogTable = () => {
    const [result] = useSuspenseObservableQuery(StreamingLogQuery);
    const entries = result.data as LogEntry[];

    return (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
                <tr>
                    <th style={{ textAlign: 'left', paddingBottom: '0.5rem', width: '160px' }}>Timestamp</th>
                    <th style={{ textAlign: 'center', paddingBottom: '0.5rem', width: '80px' }}>Level</th>
                    <th style={{ textAlign: 'left', paddingBottom: '0.5rem' }}>Message</th>
                </tr>
            </thead>
            <tbody>
                {entries.map(entry => (
                    <tr key={entry.id}>
                        <td style={{ paddingBlock: '0.4rem', fontFamily: 'var(--font-mono)', fontSize: '0.8rem' }}>
                            {entry.timestamp}
                        </td>
                        <td style={{ textAlign: 'center' }}>
                            <StoryBadge variant={levelBadgeVariant(entry.level)}>
                                {entry.level.toUpperCase()}
                            </StoryBadge>
                        </td>
                        <td style={{ paddingBlock: '0.4rem', color: 'var(--color-text-secondary)' }}>
                            {entry.message}
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
};

const FailingContent = () => {
    useSuspenseObservableQuery(FailingLogQuery);
    return null;
};

// ---------------------------------------------------------------------------
// ArcContext configuration
// ---------------------------------------------------------------------------

const arcConfig: ArcConfiguration = {
    microservice: 'stories',
    apiBasePath: '/api',
    origin: '',
};

// ---------------------------------------------------------------------------
// Storybook metadata
// ---------------------------------------------------------------------------

const meta: Meta = {
    title: 'Queries/SuspenseObservableQuery',
    parameters: {
        docs: {
            description: {
                component:
                    'Showcases `useSuspenseObservableQuery` — the Suspense-compatible variant of the observable ' +
                    'query hook. Components suspend until the first WebSocket message arrives, then stream ' +
                    'updates reactively. Errors are propagated to the nearest ErrorBoundary.',
            },
        },
    },
};

export default meta;

// ---------------------------------------------------------------------------
// Story: streaming updates — shows loading → initial data → live updates.
// ---------------------------------------------------------------------------

export const Default = {
    name: 'Streaming Updates',
    render: () => {
        const [runKey, setRunKey] = useState(0);

        const handleRerun = () => {
            StreamingLogQuery.reset();
            clearSuspenseObservableQueryCache();
            setRunKey(k => k + 1);
        };

        const handleInject = () => {
            StreamingLogQuery.push('warn', 'Manual event injected via story button');
        };

        return (
            <ArcContext.Provider value={arcConfig}>
                <StoryContainer size="md" asCard>
                    <StorySection>
                        <h2>Live Log Stream</h2>
                        <p>
                            The component suspends for ~1.5 s until the first message arrives. New entries
                            are pushed every 2 s automatically. Use the buttons to inject a manual event or
                            reset the entire stream.
                        </p>
                        <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
                            <button onClick={handleInject}>Inject event</button>
                            <button
                                onClick={handleRerun}
                                style={{ background: 'var(--color-background-tertiary)', color: 'var(--color-text)' }}
                            >
                                Re-run query
                            </button>
                        </div>
                    </StorySection>

                    <StorySection>
                        <React.Suspense fallback={<LoadingSpinner />}>
                            <LogTable key={runKey} />
                        </React.Suspense>
                    </StorySection>
                </StoryContainer>
            </ArcContext.Provider>
        );
    },
};

// ---------------------------------------------------------------------------
// Story: observable exception — QueryErrorBoundary catches QueryFailed.
// ---------------------------------------------------------------------------

export const WithQueryFailed = {
    name: 'Server Exception (QueryFailed)',
    render: () => {
        const [key, setKey] = useState(0);

        const handleReset = () => {
            clearSuspenseObservableQueryCache();
            setKey(k => k + 1);
        };

        return (
            <ArcContext.Provider value={arcConfig}>
                <StoryContainer size="sm" asCard>
                    <StorySection>
                        <h2>Observable Query with Server Exception</h2>
                        <p>
                            The first message from the observable carries <code>hasExceptions: true</code>.
                            The hook throws a <code>QueryFailed</code> error that is caught by the{' '}
                            <code>QueryErrorBoundary</code> below.
                        </p>
                    </StorySection>

                    <StorySection>
                        <QueryErrorBoundary
                            key={key}
                            onError={({ error, isQueryFailed, reset }) => (
                                <div
                                    style={{
                                        padding: '1.25rem',
                                        border: '1px solid var(--color-error)',
                                        borderRadius: 'var(--radius-md)',
                                        background: 'rgba(239,68,68,0.08)',
                                    }}
                                >
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.75rem' }}>
                                        <StoryBadge variant="error">QueryFailed</StoryBadge>
                                        <strong style={{ color: 'var(--color-error)' }}>
                                            Query encountered a server error
                                        </strong>
                                    </div>
                                    {isQueryFailed && (
                                        <ul style={{ margin: '0 0 0.75rem 1.25rem', padding: 0, color: 'var(--color-text-secondary)' }}>
                                            {(error as QueryFailed).exceptionMessages.map((m, i) => (
                                                <li key={i}>{m}</li>
                                            ))}
                                        </ul>
                                    )}
                                    <button
                                        style={{ marginTop: '0.5rem' }}
                                        onClick={() => { reset(); handleReset(); }}
                                    >
                                        Retry
                                    </button>
                                </div>
                            )}
                        >
                            <React.Suspense fallback={<LoadingSpinner />}>
                                <FailingContent key={key} />
                            </React.Suspense>
                        </QueryErrorBoundary>
                    </StorySection>
                </StoryContainer>
            </ArcContext.Provider>
        );
    },
};
