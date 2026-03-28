// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useIdentity } from '@cratis/arc.react/identity';
import { Anonymous, Authenticated } from './AuthenticationQueryItem';

/**
 * Page for verifying issue #2020 where [AllowAnonymous] queries must keep working for unauthenticated users.
 */
export const AuthenticationQueriesPage = () => {
    const identity = useIdentity();
    const [anonymousResult] = Anonymous.use();
    const [authenticatedResult] = Authenticated.when(identity.isSet).use();

    return (
        <div>
            <h2>Authentication Queries</h2>
            <p>
                This page verifies anonymous query behavior.
                The anonymous stream below should keep working while logged out.
            </p>

            <section style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16, marginBottom: 16 }}>
                <h3 style={{ marginTop: 0 }}>Anonymous Query</h3>
                <p>
                    Uses <code>AuthenticationQueryItem.Anonymous()</code> with <code>[AllowAnonymous]</code>.
                </p>
                {anonymousResult.isPerforming && <p>Connecting…</p>}
                {anonymousResult.hasData && <p><strong>{anonymousResult.data.message}</strong></p>}
            </section>

            <section style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16 }}>
                <h3 style={{ marginTop: 0 }}>Authenticated Query</h3>
                <p>
                    Uses <code>AuthenticationQueryItem.Authenticated()</code> and is only started when logged in.
                </p>
                {!identity.isSet && (
                    <p style={{ color: '#888', margin: 0 }}>
                        Log in from the top toolbar to activate this query.
                    </p>
                )}
                {identity.isSet && authenticatedResult.isPerforming && <p>Connecting…</p>}
                {identity.isSet && authenticatedResult.hasData && <p><strong>{authenticatedResult.data.message}</strong></p>}
            </section>
        </div>
    );
};
