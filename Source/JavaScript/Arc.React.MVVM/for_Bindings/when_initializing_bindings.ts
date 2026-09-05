// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from 'tsyringe';
import { Constructor } from '@cratis/fundamentals';
import { Bindings } from '../Bindings';
import { IMessenger, Messenger } from '@cratis/arc/messaging';

describe('when initializing bindings', () => {
    let resolvedMessenger: IMessenger;
    let previousLocalStorage: Storage | undefined;

    beforeEach(() => {
        previousLocalStorage = globalThis.localStorage;
        (globalThis as { localStorage: Storage; }).localStorage = {} as Storage;
        Bindings.initialize();
        resolvedMessenger = container.resolve(IMessenger as Constructor<IMessenger>);
    });

    afterEach(() => {
        if (previousLocalStorage) {
            (globalThis as { localStorage: Storage; }).localStorage = previousLocalStorage;
        } else {
            delete (globalThis as Partial<{ localStorage: Storage; }>).localStorage;
        }
        container.clearInstances();
    });

    it('should resolve the messenger from Arc core', () => resolvedMessenger.should.be.instanceOf(Messenger));
});
