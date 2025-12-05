// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from 'tsyringe';
import { Constructor } from '@cratis/fundamentals';
import { IQueryProvider, QueryProvider } from '@cratis/arc/queries';
import { Globals, GetHttpHeaders } from '@cratis/arc';
import { WellKnownBindings } from './WellKnownBindings';

export class Bindings {
    static initialize(microservice: string, apiBasePath?: string, origin?: string, httpHeadersCallback?: GetHttpHeaders): void {
        Globals.microservice = microservice;
        Globals.apiBasePath = apiBasePath ?? '';
        Globals.origin = origin ?? '';
        container.registerSingleton(WellKnownBindings.microservice, microservice);
        container.register(IQueryProvider as Constructor<IQueryProvider>, { useValue: new QueryProvider(microservice, apiBasePath ?? '', origin ?? '', httpHeadersCallback ?? (() => ({}))) });
    }
}