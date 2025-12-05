// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScope } from './commands';
import { IdentityProvider } from './identity';
import { Bindings } from './Bindings';
import { ArcConfiguration, ArcContext } from './ArcContext';
import { GetHttpHeaders } from '@cratis/arc';

/**
 * Properties for the Arc context component.
 */
export interface ArcProps {
    children?: JSX.Element | JSX.Element[];
    microservice?: string;
    development?: boolean;
    origin?: string;
    basePath?: string;
    apiBasePath?: string;
    httpHeadersCallback?: GetHttpHeaders;
}

/**
 * Arc context component.
 * @param props Props for configuring Arc
 */
export const Arc = (props: ArcProps) => {
    const configuration: ArcConfiguration = {
        microservice: props.microservice ?? '',
        development: props.development ?? false,
        origin: props.origin ?? '',
        basePath: props.basePath ?? '',
        apiBasePath: props.apiBasePath ?? '',
        httpHeadersCallback: props.httpHeadersCallback
    };

    Bindings.initialize(
        configuration.microservice,
        configuration.apiBasePath,
        configuration.origin,
        configuration.httpHeadersCallback);

    return (
        <ArcContext.Provider value={configuration}>
            <IdentityProvider httpHeadersCallback={props.httpHeadersCallback}>
                <CommandScope>
                    {props.children}
                </CommandScope>
            </IdentityProvider>
        </ArcContext.Provider>);
};