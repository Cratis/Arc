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
};

const defaultContextValue: IdentityContextValue = {
    identity: defaultIdentityContext
};

export const IdentityProviderContext = React.createContext<IdentityContextValue>(defaultContextValue);

export interface IdentityProviderProps {
    children?: JSX.Element | JSX.Element[],
    httpHeadersCallback?: GetHttpHeaders,
    detailsType?: Constructor
}

export const IdentityProvider = (props: IdentityProviderProps) => {
    const arc = useContext(ArcContext);
    
    const fetchIdentity = (): Promise<IIdentity> => {
        return RootIdentityProvider.getCurrent(props.detailsType).then(identity => {
            const wrappedIdentity = wrapRefresh(identity);
            setContext({
                identity: wrappedIdentity,
                detailsConstructor: props.detailsType
            });
            return wrappedIdentity;
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
                        setContext({
                            identity: wrappedIdentity,
                            detailsConstructor: props.detailsType
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

    const [context, setContext] = useState<IdentityContextValue>({
        identity: wrapRefresh(initialIdentity),
        detailsConstructor: props.detailsType
    });

    useEffect(() => {
        RootIdentityProvider.setHttpHeadersCallback(props.httpHeadersCallback!);
        RootIdentityProvider.setApiBasePath(arc.apiBasePath ?? '');
        RootIdentityProvider.setOrigin(arc.origin ?? '');
        fetchIdentity().catch(error => {
            console.error('Failed to fetch initial identity:', error);
        });
    }, []);

    return (
        <IdentityProviderContext.Provider value={context}>
            {props.children}
        </IdentityProviderContext.Provider>
    );
};
