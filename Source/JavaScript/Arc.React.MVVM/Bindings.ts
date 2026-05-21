// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from 'tsyringe';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { Constructor } from '@cratis/fundamentals';
import { ILocalStorage, INavigation, Navigation } from './browser';
import { IdentityProvider, IIdentityProvider } from '@cratis/arc/identity';

export class Bindings {
    static initialize() {
        container.register(IMessenger as Constructor<IMessenger>, { useValue: new Messenger() });
        container.registerSingleton(INavigation as Constructor<INavigation>, Navigation);
        container.registerSingleton(IIdentityProvider as Constructor<IIdentityProvider>, IdentityProvider);
        container.registerInstance(ILocalStorage as Constructor<ILocalStorage>, localStorage);
    }
}
