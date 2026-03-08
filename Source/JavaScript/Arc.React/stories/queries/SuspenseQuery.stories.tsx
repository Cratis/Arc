// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* eslint-disable @typescript-eslint/no-explicit-any */
import React, { Component, ErrorInfo, ReactNode, useState } from 'react';
import { Meta } from '@storybook/react';
import { QueryFor, QueryResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { useSuspenseQuery, clearSuspenseQueryCache, QueryFailed, QueryUnauthorized } from '../../queries';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { StoryContainer, StorySection, StoryBadge } from '../StoryContainer';

// ---------------------------------------------------------------------------
// Data model
// ---------------------------------------------------------------------------

interface TodoItem {
    id: string;
    title: string;
    completed: boolean;
}

// ---------------------------------------------------------------------------
// Fake query classes — override perform() to simulate network delays without
// touching real fetch infrastructure.
// ---------------------------------------------------------------------------

/** Returns 3 todo items after a 1.5 second simulated delay. */
class DelayedTodoQuery extends QueryFor<TodoItem[]> {
    readonly route = '/api/stories/todos';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: TodoItem[] = [];

    constructor() {
        super(Object, true);
    }

    get requiredRequestParameters(): string[] {
        return [];
    }

    async perform(): Promise<QueryResult<TodoItem[]>> {
        await new Promise(resolve => setTimeout(resolve, 1500));
        return new QueryResult(
            {
                data: [
                    { id: '1', title: 'Learn React Suspense', completed: true },
                    { id: '2', title: 'Use useSuspenseQuery hook', completed: true },
                    { id: '3', title: 'Add ErrorBoundary', completed: false },
                ] as unknown as object,
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 3, totalItems: 3, totalPages: 1 },
            },
            Object,
            true
        ) as QueryResult<TodoItem[]>;
    }
}

/** Simulates a server-side exception after a 1 second delay. */
class FailingTodoQuery extends QueryFor<TodoItem[]> {
    readonly route = '/api/stories/todos-error';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: TodoItem[] = [];

    constructor() {
        super(Object, true);
    }

    get requiredRequestParameters(): string[] {
        return [];
    }

    async perform(): Promise<QueryResult<TodoItem[]>> {
        await new Promise(resolve => setTimeout(resolve, 1000));
        return new QueryResult(
            {
                data: [] as unknown as object,
                isSuccess: false,
                isAuthorized: true,
                isValid: true,
                hasExceptions: true,
                validationResults: [],
                exceptionMessages: [
                    'Database connection failed',
                    'Unable to retrieve todos from storage',
                ],
                exceptionStackTrace:
                    'at TodoRepository.GetAll() in TodoRepository.cs:42\n' +
                    'at TodosQueryHandler.Execute() in TodosQueryHandler.cs:18',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
            },
            Object,
            true
        ) as QueryResult<TodoItem[]>;
    }
}

/** Simulates a 401 Unauthorized response after a 0.8 second delay. */
class UnauthorizedTodoQuery extends QueryFor<TodoItem[]> {
    readonly route = '/api/stories/todos-auth';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: TodoItem[] = [];

    constructor() {
        super(Object, true);
    }

    get requiredRequestParameters(): string[] {
        return [];
    }

    async perform(): Promise<QueryResult<TodoItem[]>> {
        await new Promise(resolve => setTimeout(resolve, 800));
        return new QueryResult(
            {
                data: [] as unknown as object,
                isSuccess: false,
                isAuthorized: false,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
            },
            Object,
            true
        ) as QueryResult<TodoItem[]>;
    }
}

// ---------------------------------------------------------------------------
// ErrorBoundary — a minimal class-based component that catches thrown errors
// from useSuspenseQuery and renders an error panel instead of crashing.
// ---------------------------------------------------------------------------

interface ErrorBoundaryState {
    error: Error | null;
}

interface ErrorBoundaryProps {
    children: ReactNode;
    onReset?: () => void;
}

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
    constructor(props: ErrorBoundaryProps) {
        super(props);
        this.state = { error: null };
    }

    static getDerivedStateFromError(error: Error): ErrorBoundaryState {
        return { error };
    }

    componentDidCatch(error: Error, info: ErrorInfo) {
        console.error('[ErrorBoundary]', error, info);
    }

    handleReset = () => {
        this.setState({ error: null });
        this.props.onReset?.();
    };

    render() {
        const { error } = this.state;
        if (error) {
            const isQueryFailed = error instanceof QueryFailed;
            const isUnauthorized = error instanceof QueryUnauthorized;
            return (
                <div
                    style={{
                        padding: '1.25rem',
                        border: '1px solid var(--color-error)',
                        borderRadius: 'var(--radius-md)',
                        background: 'rgba(239,68,68,0.08)',
                    }}
                >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.75rem' }}>
                        <StoryBadge variant="error">
                            {isUnauthorized ? 'QueryUnauthorized' : 'QueryFailed'}
                        </StoryBadge>
                        <strong style={{ color: 'var(--color-error)' }}>
                            {isUnauthorized
                                ? 'Not authorized to run this query'
                                : 'Query encountered a server error'}
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
                        onClick={this.handleReset}
                    >
                        Retry
                    </button>
                </div>
            );
        }
        return this.props.children;
    }
}

// ---------------------------------------------------------------------------
// Loading / content components
// ---------------------------------------------------------------------------

const LoadingSpinner = () => (
    <div
        style={{
            padding: '2rem',
            textAlign: 'center',
            color: 'var(--color-text-muted)',
        }}
    >
        ⏳ Loading…
    </div>
);

interface TodoListProps {
    onPerformReady: (perform: () => Promise<void>) => void;
}

const TodoList = ({ onPerformReady }: TodoListProps) => {
    const [result, perform] = useSuspenseQuery(DelayedTodoQuery);

    // Expose the perform function to the parent so the Re-run button can call it.
    React.useEffect(() => {
        onPerformReady(perform);
    }, [perform, onPerformReady]);

    const items = result.data as TodoItem[];
    return (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
                <tr>
                    <th style={{ textAlign: 'left', paddingBottom: '0.5rem' }}>Title</th>
                    <th style={{ textAlign: 'center', paddingBottom: '0.5rem' }}>Status</th>
                </tr>
            </thead>
            <tbody>
                {items.map(item => (
                    <tr key={item.id}>
                        <td style={{ paddingBlock: '0.4rem' }}>{item.title}</td>
                        <td style={{ textAlign: 'center' }}>
                            <StoryBadge variant={item.completed ? 'success' : 'warning'}>
                                {item.completed ? 'Done' : 'Pending'}
                            </StoryBadge>
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
};

const ErrorContent = () => {
    useSuspenseQuery(FailingTodoQuery);
    return null;
};

const UnauthorizedContent = () => {
    useSuspenseQuery(UnauthorizedTodoQuery);
    return null;
};

// ---------------------------------------------------------------------------
// ArcContext configuration — not used for real HTTP calls since perform() is
// overridden, but required by the hook.
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
    title: 'Queries/SuspenseQuery',
    parameters: {
        docs: {
            description: {
                component:
                    'Showcases `useSuspenseQuery` — the Suspense-compatible variant of the query hook. ' +
                    'Components suspend while the query is in-flight and errors are propagated to the nearest ErrorBoundary.',
            },
        },
    },
};

export default meta;

// ---------------------------------------------------------------------------
// Story: happy path — shows loading spinner, then data, with a Re-run button.
// ---------------------------------------------------------------------------

export const Default = {
    name: 'Happy Path',
    render: () => {
        const performRef = React.useRef<() => Promise<void>>(async () => {});
        const [runKey, setRunKey] = useState(0);

        const handleRerun = () => {
            clearSuspenseQueryCache();
            setRunKey(k => k + 1);
        };

        return (
            <ArcContext.Provider value={arcConfig}>
                <StoryContainer size="sm" asCard>
                    <StorySection>
                        <h2>Todo List</h2>
                        <p>
                            The component suspends for ~1.5 s while the query resolves, then renders the
                            data. Click <strong>Re-run</strong> to replay the loading sequence.
                        </p>
                        <button onClick={handleRerun}>Re-run query</button>
                    </StorySection>

                    <StorySection>
                        <React.Suspense fallback={<LoadingSpinner />}>
                            <TodoList
                                key={runKey}
                                onPerformReady={p => { performRef.current = p; }}
                            />
                        </React.Suspense>
                    </StorySection>
                </StoryContainer>
            </ArcContext.Provider>
        );
    },
};

// ---------------------------------------------------------------------------
// Story: server exception — ErrorBoundary catches QueryFailed.
// ---------------------------------------------------------------------------

export const WithQueryFailed = {
    name: 'Server Exception (QueryFailed)',
    render: () => {
        const [key, setKey] = useState(0);

        const handleReset = () => {
            clearSuspenseQueryCache();
            setKey(k => k + 1);
        };

        return (
            <ArcContext.Provider value={arcConfig}>
                <StoryContainer size="sm" asCard>
                    <StorySection>
                        <h2>Query with Server Exception</h2>
                        <p>
                            The query returns <code>hasExceptions: true</code>. The hook throws a{' '}
                            <code>QueryFailed</code> error that is caught by the{' '}
                            <code>ErrorBoundary</code> below.
                        </p>
                    </StorySection>

                    <StorySection>
                        <ErrorBoundary key={key} onReset={handleReset}>
                            <React.Suspense fallback={<LoadingSpinner />}>
                                <ErrorContent key={key} />
                            </React.Suspense>
                        </ErrorBoundary>
                    </StorySection>
                </StoryContainer>
            </ArcContext.Provider>
        );
    },
};

// ---------------------------------------------------------------------------
// Story: unauthorized — ErrorBoundary catches QueryUnauthorized.
// ---------------------------------------------------------------------------

export const WithUnauthorized = {
    name: 'Unauthorized (QueryUnauthorized)',
    render: () => {
        const [key, setKey] = useState(0);

        const handleReset = () => {
            clearSuspenseQueryCache();
            setKey(k => k + 1);
        };

        return (
            <ArcContext.Provider value={arcConfig}>
                <StoryContainer size="sm" asCard>
                    <StorySection>
                        <h2>Unauthorized Query</h2>
                        <p>
                            The query returns <code>isAuthorized: false</code>. The hook throws a{' '}
                            <code>QueryUnauthorized</code> error that is caught by the{' '}
                            <code>ErrorBoundary</code> below.
                        </p>
                    </StorySection>

                    <StorySection>
                        <ErrorBoundary key={key} onReset={handleReset}>
                            <React.Suspense fallback={<LoadingSpinner />}>
                                <UnauthorizedContent key={key} />
                            </React.Suspense>
                        </ErrorBoundary>
                    </StorySection>
                </StoryContainer>
            </ArcContext.Provider>
        );
    },
};
