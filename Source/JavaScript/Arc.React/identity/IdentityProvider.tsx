// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { useState, useEffect, useContext } from 'react';
import { Constructor } from '@cratis/fundamentals';
import { IIdentity } from '@cratis/arc/identity';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { GetHttpHeaders } from '@cratis/arc';
import { ArcContext } from '../ArcContext';

const defaultIdentityContext: IIdentity = {
    id: '',
    name: '',
    roles: [],
    details: {},
    isSet: false,
    isInRole: () => false,
    refresh: () => {
        return new Promise((resolve, reject) => {
            reject('Not implemented');
        });
    }
};

type IdentityContextValue = {
    identity: IIdentity;
    detailsConstructor?: Constructor;

    /**
     * Whether the identity is still being resolved.
     *
     * Optional here, and required on {@link IIdentityContext}, on purpose. This shape reaches consumers
     * through the exported context, so anything a test harness or a Storybook decorator builds by hand
     * would stop compiling if the field were required - while a consumer reading the identity should
     * never have to answer "and what if it is absent?". {@link useIdentity} bridges the two.
     *
     * Absent reads as resolved, which is the safe direction: a gate seeing a hand-built context denies
     * rather than admits.
     */
    isLoading?: boolean;
    clearIdentity: () => void;
};

const defaultContextValue: IdentityContextValue = {
    identity: defaultIdentityContext,
    isLoading: false,
    clearIdentity: () => { /* no-op until provider initializes */ },
};

export const IdentityProviderContext = React.createContext<IdentityContextValue>(defaultContextValue);

export interface IdentityProviderProps {
    children?: JSX.Element | JSX.Element[],
    httpHeadersCallback?: GetHttpHeaders,
    detailsType?: Constructor
}

export const IdentityProvider = (props: IdentityProviderProps) => {
    const arc = useContext(ArcContext);

    // Keep the root identity provider's settings in sync on every render
    // so that identity.refresh() always uses the latest callback and paths.
    RootIdentityProvider.setHttpHeadersCallback(props.httpHeadersCallback ?? (() => ({})));
    RootIdentityProvider.setApiBasePath(arc.apiBasePath ?? '');
    RootIdentityProvider.setOrigin(arc.origin ?? '');

    // Every identity request re-enters loading, not just the first one. Until the answer arrives the
    // identity in state is the previous one - or an unset one - and reporting that as settled is what
    // makes a signed-in user flash the signed-out UI for the length of the round-trip. Returning the
    // current state unchanged when it is already loading lets React bail out of the re-render, so
    // this can be called on every request without a render loop.
    const beginLoading = (): void => {
        setIdentityState(current => current.isLoading ? current : { ...current, isLoading: true });
    };

    // A request that is over - however it ended - must never leave consumers stranded in their
    // loading state for the rest of the session.
    const stopLoading = (): void => {
        setIdentityState(current => current.isLoading ? { ...current, isLoading: false } : current);
    };

    const fetchIdentity = (): Promise<IIdentity> => {
        beginLoading();
        return RootIdentityProvider.getCurrent(props.detailsType).then(identity => {
            const wrappedIdentity = wrapRefresh(identity);
            setIdentityState({
                identity: wrappedIdentity,
                detailsConstructor: props.detailsType,
                isLoading: false
            });
            return wrappedIdentity;
        });
    };

    const clearIdentity = (): void => {
        RootIdentityProvider.clearIdentityCookie();
        setIdentityState({
            identity: wrapRefresh(initialIdentity),
            detailsConstructor: props.detailsType,
            isLoading: false
        });
    };

    const wrapRefresh = (identity: IIdentity): IIdentity => {
        const originalRefresh = identity.refresh.bind(identity);
        return {
            ...identity,
            refresh: () => {
                return new Promise<IIdentity>((resolve, reject) => {
                    beginLoading();
                    originalRefresh().then(newIdentity => {
                        const wrappedIdentity = wrapRefresh(newIdentity);
                        setIdentityState({
                            identity: wrappedIdentity,
                            detailsConstructor: props.detailsType,
                            isLoading: false
                        });
                        resolve(wrappedIdentity);
                    }).catch(error => {
                        // The identity that is still in state is the one from before the refresh, and it
                        // is now suspect - the cookie it came from was cleared before the request went
                        // out. Settling the question is all this can do; the caller decides what a failed
                        // refresh means for the session.
                        stopLoading();
                        reject(error);
                    });
                });
            }
        };
    };

    const initialIdentity: IIdentity = {
        id: '',
        name: '',
        roles: [],
        details: {},
        isSet: false,
        isInRole: () => false,
        refresh: () => fetchIdentity()
    };

    // Seeded as loading: the first fetch has not answered yet, and until it does an unset identity
    // must not be reported as anonymous - see IIdentityContext.isLoading.
    const [identityState, setIdentityState] = useState<{ identity: IIdentity; detailsConstructor?: Constructor; isLoading: boolean }>({
        identity: wrapRefresh(initialIdentity),
        detailsConstructor: props.detailsType,
        isLoading: true,
    });

    useEffect(() => {
        fetchIdentity().catch(error => {
            console.error('Failed to fetch initial identity:', error);
            // A failed fetch still settles the question - the identity is not going to arrive.
            stopLoading();
        });
    }, []);

    const contextValue: IdentityContextValue = {
        ...identityState,
        clearIdentity,
    };

    return (
        <IdentityProviderContext.Provider value={contextValue}>
            {props.children}
        </IdentityProviderContext.Provider>
    );
};
