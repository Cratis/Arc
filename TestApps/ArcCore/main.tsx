// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode, useState, useContext, useRef, useEffect } from 'react';
import { createRoot } from 'react-dom/client';
import { Arc, ArcContext } from '@cratis/arc.react';
import { useIdentity } from '@cratis/arc.react/identity';
import { App, Page } from '../Shared/App';
import { QueryTransportMethod } from '@cratis/arc/queries';
import { ObservableQueryTransferMode } from '@cratis/arc';

const storageKeys = {
    transport: 'arccore.transport',
    connectionCount: 'arccore.connectionCount',
    directMode: 'arccore.directMode',
    transferMode: 'arccore.transferMode',
    loggedIn: 'arccore.loggedIn',
    userId: 'arccore.userId',
    userName: 'arccore.userName',
    roles: 'arccore.roles',
};

const getString = (key: string, fallback: string): string => {
    const value = localStorage.getItem(key);
    return value ?? fallback;
};

const getNumber = (key: string, fallback: number): number => {
    const value = Number.parseInt(localStorage.getItem(key) ?? '', 10);
    return Number.isNaN(value) ? fallback : value;
};

const getBoolean = (key: string, fallback: boolean): boolean => {
    const value = localStorage.getItem(key);
    if (value === null) {
        return fallback;
    }
    return value === 'true';
};

const getTransport = (): QueryTransportMethod => {
    const value = getString(storageKeys.transport, QueryTransportMethod.WebSocket);
    return value === QueryTransportMethod.ServerSentEvents ? QueryTransportMethod.ServerSentEvents : QueryTransportMethod.WebSocket;
};

const getTransferMode = (): ObservableQueryTransferMode => {
    const value = getString(storageKeys.transferMode, ObservableQueryTransferMode.Delta);
    return value === ObservableQueryTransferMode.Full ? ObservableQueryTransferMode.Full : ObservableQueryTransferMode.Delta;
};

const persistSettings = (
    transport: QueryTransportMethod,
    connectionCount: number,
    directMode: boolean,
    transferMode: ObservableQueryTransferMode,
    loggedIn: boolean,
    userId: string,
    userName: string,
    roles: string,
) => {
    localStorage.setItem(storageKeys.transport, transport);
    localStorage.setItem(storageKeys.connectionCount, connectionCount.toString());
    localStorage.setItem(storageKeys.directMode, directMode.toString());
    localStorage.setItem(storageKeys.transferMode, transferMode);
    localStorage.setItem(storageKeys.loggedIn, loggedIn.toString());
    localStorage.setItem(storageKeys.userId, userId);
    localStorage.setItem(storageKeys.userName, userName);
    localStorage.setItem(storageKeys.roles, roles);
};

const encodeClientPrincipal = (userId: string, userName: string, roles: string): string => {
    const rolesList = roles.split(',').filter(r => r.trim()).map(r => r.trim());
    const clientPrincipal = {
        identityProvider: 'test',
        userId: userId,
        userDetails: userName,
        userRoles: rolesList,
        claims: [],
    };
    const json = JSON.stringify(clientPrincipal);
    return btoa(json);
};

const setAuthCookie = (userId: string, userName: string, roles: string): void => {
    const encoded = encodeClientPrincipal(userId, userName, roles);
    document.cookie = `x-ms-client-principal=${encoded}; path=/; SameSite=Lax`;
};

const clearAuthCookie = (): void => {
    document.cookie = 'x-ms-client-principal=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT';
};

/**
 * Bridges auth-state changes into the Arc context.
 * When loggedIn or credential fields change, this component calls
 * reconnectQueries() to re-establish transport connections with the
 * current authentication cookies.
 * On logout, it also calls identity.clearIdentity() to reset identity state.
 */
const AuthBridge = ({ loggedIn, userId, userName, roles }: { loggedIn: boolean; userId: string; userName: string; roles: string }) => {
    const arc = useContext(ArcContext);
    const identity = useIdentity();
    const initialized = useRef(false);
    const prevLoggedIn = useRef(loggedIn);

    useEffect(() => {
        if (!initialized.current) {
            initialized.current = true;
            return;
        }

        if (!loggedIn && !prevLoggedIn.current) {
            prevLoggedIn.current = loggedIn;
            return;
        }

        if (!loggedIn) {
            identity.clearIdentity();
        } else {
            identity.refresh();
        }
        prevLoggedIn.current = loggedIn;
        arc.reconnectQueries?.();
    }, [loggedIn, userId, userName, roles]);

    return null;
};

const Root = () => {
    const [transport, setTransport] = useState<QueryTransportMethod>(() => getTransport());
    const [connectionCount, setConnectionCount] = useState(() => Math.max(1, Math.min(10, getNumber(storageKeys.connectionCount, 1))));
    const [directMode, setDirectMode] = useState(() => getBoolean(storageKeys.directMode, false));
    const [transferMode, setTransferMode] = useState<ObservableQueryTransferMode>(() => getTransferMode());
    const [loggedIn, setLoggedIn] = useState(() => getBoolean(storageKeys.loggedIn, false));
    const [userId, setUserId] = useState(() => getString(storageKeys.userId, 'demo-user'));
    const [userName, setUserName] = useState(() => getString(storageKeys.userName, 'Demo User'));
    const [roles, setRoles] = useState(() => getString(storageKeys.roles, 'User'));
    const [configKey, setConfigKey] = useState(0);
    const [page, setPage] = useState<Page>('authenticationqueries');

    useEffect(() => {
        if (loggedIn) {
            setAuthCookie(userId, userName, roles);
        } else {
            // Ensure browser auth state is deterministic on load and refresh.
            clearAuthCookie();
        }
    }, []);

    const apply = (
        newTransport: QueryTransportMethod,
        newCount: number,
        newDirect: boolean,
        newLoggedIn: boolean,
        newUserId: string,
        newUserName: string,
        newRoles: string,
        newTransferMode: ObservableQueryTransferMode = transferMode,
    ) => {
        const configChanged = newTransport !== transport || newCount !== connectionCount || newDirect !== directMode || newTransferMode !== transferMode;

        setTransport(newTransport);
        setConnectionCount(newCount);
        setDirectMode(newDirect);
        setTransferMode(newTransferMode);
        setLoggedIn(newLoggedIn);
        setUserId(newUserId);
        setUserName(newUserName);
        setRoles(newRoles);

        if (newLoggedIn) {
            setAuthCookie(newUserId, newUserName, newRoles);
        } else {
            clearAuthCookie();
        }

        persistSettings(newTransport, newCount, newDirect, newTransferMode, newLoggedIn, newUserId, newUserName, newRoles);

        if (configChanged) {
            setConfigKey(current => current + 1);
        }
    };

    const httpHeadersCallback = (): Record<string, string> => {
        if (!loggedIn) {
            return {};
        }

        return {
            'x-ms-client-principal-id': userId,
            'x-ms-client-principal-name': userName,
            'x-ms-client-principal': encodeClientPrincipal(userId, userName, roles),
        };
    };

    return (
        <StrictMode>
            <div style={{ padding: '8px 16px', background: '#f0f0f0', borderBottom: '1px solid #ccc', display: 'flex', gap: 16, alignItems: 'center', fontSize: 14, flexWrap: 'wrap' }}>
                <label>
                    Transport{' '}
                    <select value={transport} onChange={event => apply(event.target.value as QueryTransportMethod, connectionCount, directMode, loggedIn, userId, userName, roles)}>
                        <option value={QueryTransportMethod.WebSocket}>WebSocket</option>
                        <option value={QueryTransportMethod.ServerSentEvents}>Server-Sent Events</option>
                    </select>
                </label>
                <label style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    Connections{' '}
                    <button onClick={() => apply(transport, Math.max(1, connectionCount - 1), directMode, loggedIn, userId, userName, roles)} style={{ width: 24, cursor: 'pointer' }}>-</button>
                    <span style={{ minWidth: 20, textAlign: 'center' }}>{connectionCount}</span>
                    <button onClick={() => apply(transport, Math.min(10, connectionCount + 1), directMode, loggedIn, userId, userName, roles)} style={{ width: 24, cursor: 'pointer' }}>+</button>
                </label>
                <label>
                    <input type="checkbox" checked={directMode} onChange={event => apply(transport, connectionCount, event.target.checked, loggedIn, userId, userName, roles)} />{' '}
                    Direct mode
                </label>
                <label>
                    Transfer mode{' '}
                    <select value={transferMode} onChange={event => apply(transport, connectionCount, directMode, loggedIn, userId, userName, roles, event.target.value as ObservableQueryTransferMode)}>
                        <option value={ObservableQueryTransferMode.Delta}>Delta</option>
                        <option value={ObservableQueryTransferMode.Full}>Full</option>
                    </select>
                </label>
                <label>
                    <input type="checkbox" checked={loggedIn} onChange={event => apply(transport, connectionCount, directMode, event.target.checked, userId, userName, roles)} />{' '}
                    Logged in
                </label>
                <label>
                    User ID{' '}
                    <input value={userId} onChange={event => apply(transport, connectionCount, directMode, loggedIn, event.target.value, userName, roles)} style={{ width: 120 }} />
                </label>
                <label>
                    Name{' '}
                    <input value={userName} onChange={event => apply(transport, connectionCount, directMode, loggedIn, userId, event.target.value, roles)} style={{ width: 140 }} />
                </label>
                <label>
                    Roles{' '}
                    <input value={roles} onChange={event => apply(transport, connectionCount, directMode, loggedIn, userId, userName, event.target.value)} style={{ width: 120 }} />
                </label>
            </div>
            <Arc
                development={true}
                key={configKey}
                queryTransportMethod={transport}
                queryConnectionCount={connectionCount}
                queryDirectMode={directMode}
                observableQueryTransferMode={transferMode}
                httpHeadersCallback={httpHeadersCallback}
            >
                <AuthBridge loggedIn={loggedIn} userId={userId} userName={userName} roles={roles} />
                <App title="ArcCore TestApp" page={page} onPageChange={setPage} />
            </Arc>
        </StrictMode>
    );
};

createRoot(document.getElementById('root')!).render(<Root />);
