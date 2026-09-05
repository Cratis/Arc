// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, RenderResult, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { IdentityProvider } from '../../IdentityProvider';
import { IIdentityContext } from '../../IIdentityContext';
import { RequireRole, RequireRoleProps, IdentityPredicate } from '../../RequireRole';
import { useIdentity } from '../../useIdentity';
import { ArcContext } from '../../../ArcContext';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

export const allowedText = 'allowed';
export const forbiddenText = 'forbidden';
export const loadingText = 'loading';

const resolvedText = 'resolved';
const resolvingText = 'resolving';
const gateTestId = 'gate';
const probeTestId = 'probe';

/**
 * The props a spec hands to the gate. Deliberately looser than {@link RequireRoleProps}: the runtime
 * guards exist for exactly the configurations the type system rejects - no access rule at all, a
 * `roles` that turned out to be `undefined` or was never an array - and a spec has to be able to
 * build one to show what the gate does with it.
 */
export type GateProps = {
    roles?: unknown;
    allow?: IdentityPredicate<Record<string, unknown>>;
    whileLoading?: React.ReactNode;
    forbidden?: React.ReactNode;
};

/**
 * Renders a {@link RequireRole} inside a real {@link IdentityProvider} whose identity fetch is held
 * open, so a spec can observe the gate before the identity arrives as well as after.
 *
 * The identity is also rendered by a probe next to the gate. The probe is what the harness waits on:
 * it reports the provider's own answer to "have you resolved yet", which is true regardless of what
 * the gate was configured to render - and unlike reading the gate's text, it is never already true
 * before the fetch is answered.
 */
export class a_role_gate {
    renderResult!: RenderResult;
    fetchHelper: ReturnType<typeof createFetchHelper>;
    fetchStub!: sinon.SinonStub;
    capturedIdentity: IIdentityContext | null = null;
    private originalApiBasePath = '';
    private originalOrigin = '';
    private originalConsoleWarn?: typeof console.warn;
    private originalConsoleError?: typeof console.error;
    private settle!: (response: Response) => void;
    private fail!: (reason: Error) => void;

    constructor() {
        this.fetchHelper = createFetchHelper();
    }

    renderGate(props: GateProps) {
        // Everything here is per spec, not per suite: cleanup() runs after every it, so setup done
        // once in the constructor would be gone from the second it onwards.
        this.captureGlobals();

        // The fetch has to be held open per spec too - otherwise the second spec in a suite renders
        // against a restored, real fetch.
        this.fetchStub = this.fetchHelper.stubFetch();
        this.fetchStub.returns(this.heldResponse());

        const probe = <IdentityProbe onIdentity={identity => this.capturedIdentity = identity} />;

        this.renderResult = render(
            <ArcContext.Provider value={{ microservice: 'test-microservice', apiBasePath: '/api', origin: 'http://localhost' }}>
                <IdentityProvider>
                    {probe}
                    <div data-testid={gateTestId}>
                        <RequireRole<Record<string, unknown>> {...(props as unknown as RequireRoleProps<Record<string, unknown>>)}>
                            <span>{allowedText}</span>
                        </RequireRole>
                    </div>
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
        await this.waitUntilTheIdentityIsResolved();
    }

    /**
     * Answers the held identity fetch with a signed-in identity the server carries no details for.
     */
    async signInWithoutDetails(roles: string[]) {
        this.settle({
            ok: true,
            json: async () => ({ id: 'user-1', name: 'A User', roles })
        } as Response);
        await this.waitUntilTheIdentityIsResolved();
    }

    /**
     * Answers the held identity fetch with a signed-in identity whose details are explicitly null.
     *
     * Distinct from {@link signInWithoutDetails}: an absent key deserializes to undefined, an explicit
     * null stays null, and a guard written against only one of them lets the other through.
     */
    async signInWithNullDetails(roles: string[]) {
        this.settle({
            ok: true,
            json: async () => ({ id: 'user-1', name: 'A User', roles, details: null })
        } as Response);
        await this.waitUntilTheIdentityIsResolved();
    }

    /**
     * Answers the held identity fetch the way the server answers an unauthenticated caller.
     */
    async stayAnonymous() {
        this.settle({ ok: false } as Response);
        await this.waitUntilTheIdentityIsResolved();
    }

    /**
     * Fails the held identity fetch outright, the way a network error does.
     *
     * Nothing is waited for beyond the promise chain draining - what a failed load leaves behind is
     * the whole point of the specs that call this, so it must not be assumed here.
     */
    async failToLoad() {
        this.suppressConsoleErrors();
        this.fail(new Error('Failed to fetch'));
        await this.flush();
    }

    /**
     * Starts a refresh and leaves it in flight, holding the next fetch open.
     */
    async beginRefresh() {
        this.fetchStub.returns(this.heldResponse());
        await act(async () => {
            this.capturedIdentity!.refresh().catch(() => { /* left in flight on purpose */ });
            await this.drainPromises();
        });
    }

    get text() {
        return this.renderResult.getByTestId(gateTestId).textContent ?? '';
    }

    cleanup() {
        this.restoreGlobals();
        this.fetchHelper.restore();
        if (this.renderResult) {
            this.renderResult.unmount();
        }
    }

    private heldResponse() {
        return new Promise<Response>((resolve, reject) => {
            this.settle = resolve;
            this.fail = reject;
        });
    }

    private async waitUntilTheIdentityIsResolved() {
        await this.flush();
        await waitFor(() => {
            this.probeText.should.equal(resolvedText);
        }, { timeout: 1000 });
    }

    /**
     * Drains the promise chain the answered fetch feeds and lets React commit whatever came of it.
     */
    private async flush() {
        await act(async () => {
            await this.drainPromises();
        });
    }

    private drainPromises() {
        return new Promise(resolve => setTimeout(resolve, 0));
    }

    private get probeText() {
        return this.renderResult.getByTestId(probeTestId).textContent ?? '';
    }

    private captureGlobals() {
        // IdentityProvider re-sets both of these from ArcContext on every render, so there is nothing
        // useful to set here - only something to put back afterwards.
        this.originalApiBasePath = RootIdentityProvider.apiBasePath;
        this.originalOrigin = RootIdentityProvider.origin;

        // A misconfigured gate warns, by design. Capturing keeps that out of the spec output without
        // hiding warnings from the specs that never provoke one.
        this.originalConsoleWarn = console.warn;
        console.warn = () => { /* Suppressed during test */ };
    }

    private suppressConsoleErrors() {
        this.originalConsoleError = console.error;
        console.error = () => { /* Suppressed during test */ };
    }

    private restoreGlobals() {
        RootIdentityProvider.setApiBasePath(this.originalApiBasePath);
        RootIdentityProvider.setOrigin(this.originalOrigin);
        if (this.originalConsoleWarn) {
            console.warn = this.originalConsoleWarn;
            this.originalConsoleWarn = undefined;
        }
        if (this.originalConsoleError) {
            console.error = this.originalConsoleError;
            this.originalConsoleError = undefined;
        }
    }
}

const IdentityProbe = ({ onIdentity }: { onIdentity: (identity: IIdentityContext) => void }) => {
    const identity = useIdentity();
    onIdentity(identity);
    return <span data-testid={probeTestId}>{identity.isLoading ? resolvingText : resolvedText}</span>;
};
