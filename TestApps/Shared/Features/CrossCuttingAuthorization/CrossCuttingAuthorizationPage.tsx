// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { RunSecuredCommand } from './RunSecuredCommand';
import { Secured } from './CrossCuttingAuthorizationStatus';

/**
 * Page for showcasing custom cross-cutting authorization through command and query filters.
 */
export const CrossCuttingAuthorizationPage = () => {
    const [command, setValues] = RunSecuredCommand.use({ message: 'Hello from the secured command' });
    const [commandResultMessage, setCommandResultMessage] = useState('');
    const [commandError, setCommandError] = useState('');
    const [queryResult] = Secured.use();
    const hasQueryData = !!queryResult.data?.message && !!queryResult.data?.checkedAt;

    const runCommand = async () => {
        setCommandResultMessage('');
        setCommandError('');

        const result = await command.execute();
        if (result.isSuccess) {
            setCommandResultMessage(result.response ?? 'Command succeeded.');
            return;
        }

        if (!result.isAuthorized) {
            setCommandError(result.authorizationFailureReason || 'Command was denied by the custom command filter.');
            return;
        }

        setCommandError('Command failed for a reason other than authorization.');
    };

    return (
        <div>
            <h2>Cross-Cutting Authorization</h2>
            <p>
                This page demonstrates how to build your own <code>ICommandFilter</code> and <code>IQueryFilter</code> to apply
                authorization cross-cuttingly by namespace and role.
            </p>
            <p>
                The filters protect all commands and queries in <code>TestApps.Features.CrossCuttingAuthorization</code> and require
                the <code>CrossCuttingAuthorization</code> role.
            </p>

            <section style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16, marginBottom: 16 }}>
                <h3 style={{ marginTop: 0 }}>Secured Query</h3>
                <p>
                    Query class: <code>CrossCuttingAuthorizationStatus.Secured()</code>
                </p>
                {queryResult.isPerforming && <p>Loading secured query…</p>}
                {!queryResult.isPerforming && !queryResult.isAuthorized && (
                    <p style={{ color: 'red', margin: 0 }}>
                        Unauthorized by custom query filter. Add role <code>CrossCuttingAuthorization</code> in the toolbar.
                    </p>
                )}
                {!queryResult.isPerforming && queryResult.isAuthorized && hasQueryData && (
                    <p style={{ margin: 0 }}>
                        <strong>{queryResult.data.message}</strong>
                        <br />
                        <span style={{ color: '#666' }}>{new Date(queryResult.data.checkedAt).toLocaleTimeString()}</span>
                    </p>
                )}
                {!queryResult.isPerforming && queryResult.isAuthorized && !hasQueryData && (
                    <p style={{ color: '#666', margin: 0 }}>
                        No secured query data yet.
                    </p>
                )}
            </section>

            <section style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16 }}>
                <h3 style={{ marginTop: 0 }}>Secured Command</h3>
                <p>
                    Command class: <code>RunSecuredCommand</code>
                </p>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 8 }}>
                    <input
                        value={command.message ?? ''}
                        onChange={event => setValues({ message: event.target.value })}
                        style={{ minWidth: 360 }}
                    />
                    <button onClick={runCommand} disabled={!command.message?.trim()}>
                        Execute secured command
                    </button>
                </div>
                {commandResultMessage && <p style={{ color: 'green', margin: 0 }}>{commandResultMessage}</p>}
                {commandError && <p style={{ color: 'red', margin: 0 }}>{commandError}</p>}
            </section>
        </div>
    );
};
