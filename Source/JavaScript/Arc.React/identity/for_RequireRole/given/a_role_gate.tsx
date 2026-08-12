// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, RenderResult, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { IdentityProvider } from '../../IdentityProvider';
import { RequireRole, RequireRoleProps } from '../../RequireRole';
import { ArcContext } from '../../../ArcContext';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

export const allowedText = 'allowed';
export const forbiddenText = 'forbidden';
export const loadingText = 'loading';

/**
 * Renders a {@link RequireRole} inside a real {@link IdentityProvider} whose identity fetch is held
 * open, so a spec can observe the gate before the identity arrives as well as after.
 */
export class a_role_gate {
    renderResult!: RenderResult;
    fetchHelper: ReturnType<typeof createFetchHelper>;
    fetchStub!: sinon.SinonStub;
    originalApiBasePath = '';
    originalOrigin = '';
    private settle!: (response: Response) => void;

    constructor() {
        this.originalApiBasePath = RootIdentityProvider.apiBasePath;
        this.originalOrigin = RootIdentityProvider.origin;

        RootIdentityProvider.setOrigin('https://example.com');
        RootIdentityProvider.setApiBasePath('https://example.com/api');

        this.fetchHelper = createFetchHelper();
    }

    renderGate(props: Omit<RequireRoleProps<Record<string, unknown>>, 'children'>) {
        // The context is constructed once per describe, so the fetch has to be held open per spec -
        // otherwise the second spec in a suite renders against a restored, real fetch.
        this.fetchStub = this.fetchHelper.stubFetch();
        this.fetchStub.returns(new Promise<Response>(resolve => {
            this.settle = resolve;
        }));

        this.renderResult = render(
            <ArcContext.Provider value={{ microservice: 'test-microservice', apiBasePath: '/api', origin: 'http://localhost' }}>
                <IdentityProvider>
                    <RequireRole<Record<string, unknown>> {...props}>
                        <span>{allowedText}</span>
                    </RequireRole>
                </IdentityProvider>
            </ArcContext.Provider>
        );
    }

    /**
     * Answers the held identity fetch with a signed-in identity.
     */
    async signIn(roles: string[], details: Record<string, unknown> = {}) {
        this.settle({
            ok: true,
            json: async () => ({ id: 'user-1', name: 'A User', roles, details })
        } as Response);
        await this.waitUntilSettled();
    }

    /**
     * Answers the held identity fetch the way the server answers an unauthenticated caller.
     */
    async stayAnonymous() {
        this.settle({ ok: false } as Response);
        await this.waitUntilSettled();
    }

    private async waitUntilSettled() {
        await waitFor(() => {
            this.text.should.not.equal(loadingText);
        }, { timeout: 1000 });
    }

    get text() {
        return this.renderResult.container.textContent ?? '';
    }

    cleanup() {
        this.fetchHelper.restore();
        RootIdentityProvider.setApiBasePath(this.originalApiBasePath);
        RootIdentityProvider.setOrigin(this.originalOrigin);
        if (this.renderResult) {
            this.renderResult.unmount();
        }
    }
}
