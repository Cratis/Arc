// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { useOnMessage } from '../useOnMessage';

class MessageToSend {
    constructor(readonly content: string) {
    }
}

class UnrelatedMessage {
    constructor(readonly value: number) {
    }
}

describe('when publishing message of different type', () => {
    let rootMessenger: IMessenger;
    let rootConfig: ArcConfiguration;
    let received: MessageToSend[];

    beforeEach(() => {
        rootMessenger = new Messenger();
        rootConfig = {
            microservice: 'test-microservice',
            messenger: rootMessenger
        };
        received = [];

        const Subscriber = () => {
            useOnMessage(MessageToSend, message => received.push(message));
            return null;
        };

        render(
            <ArcContext.Provider value={rootConfig}>
                <Subscriber />
            </ArcContext.Provider>
        );

        rootMessenger.publish(new UnrelatedMessage(42));
    });

    it('should not invoke callback for non-matching type', () => {
        received.length.should.equal(0);
    });
});
