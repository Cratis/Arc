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
    isLoading: boolean;
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

    const fetchIdentity = (): Promise<IIdentity> => {
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
                    originalRefresh().then(newIdentity => {
                        const wrappedIdentity = wrapRefresh(newIdentity);
                        setIdentityState({
                            identity: wrappedIdentity,
                            detailsConstructor: props.detailsType,
                            isLoading: false
                        });
                        resolve(wrappedIdentity);
                    }).catch(reject);
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
            setIdentityState(current => ({ ...current, isLoading: false }));
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
