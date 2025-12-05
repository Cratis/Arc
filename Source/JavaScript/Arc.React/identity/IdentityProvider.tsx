// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { useState, useEffect, useContext } from 'react';
import { IIdentity } from '@cratis/arc/identity';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { GetHttpHeaders } from '@cratis/arc';
import { ArcContext } from '../ArcContext';

const defaultIdentityContext: IIdentity = {
    id: '',
    name: '',
    details: {},
    isSet: false,
    refresh: () => {
        return new Promise((resolve, reject) => {
            reject('Not implemented');
        });
    }
};

export const IdentityProviderContext = React.createContext<IIdentity>(defaultIdentityContext);

export interface IdentityProviderProps {
    children?: JSX.Element | JSX.Element[],
    httpHeadersCallback?: GetHttpHeaders
}

export const IdentityProvider = (props: IdentityProviderProps) => {
    const arc = useContext(ArcContext);
    const [context, setContext] = useState<IIdentity>(defaultIdentityContext);

    const wrapRefresh = (identity: IIdentity): IIdentity => {
        const originalRefresh = identity.refresh.bind(identity);
        return {
            ...identity,
            refresh: () => {
                return new Promise<IIdentity>(resolve => {
                    originalRefresh().then(newIdentity => {
                        const wrappedIdentity = wrapRefresh(newIdentity);
                        setContext(wrappedIdentity);
                        resolve(wrappedIdentity);
                    });
                });
            }
        };
    };

    useEffect(() => {
        RootIdentityProvider.setHttpHeadersCallback(props.httpHeadersCallback!);
        RootIdentityProvider.setApiBasePath(arc.apiBasePath ?? '');
        RootIdentityProvider.setOrigin(arc.origin ?? '');
        RootIdentityProvider.getCurrent().then(identity => {
            const wrappedIdentity = wrapRefresh(identity);
            setContext(wrappedIdentity);
        });
    }, []);

    return (
        <IdentityProviderContext.Provider value={context}>
            {props.children}
        </IdentityProviderContext.Provider>
    );
};
