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

describe('when component unmounts', () => {
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

        const result = render(
            <ArcContext.Provider value={rootConfig}>
                <Subscriber />
            </ArcContext.Provider>
        );

        rootMessenger.publish(new MessageToSend('before unmount'));
        result.unmount();
        rootMessenger.publish(new MessageToSend('after unmount'));
    });

    it('should receive messages published before unmount', () => {
        received.length.should.equal(1);
    });

    it('should not receive messages after unmount', () => {
        received[0].content.should.equal('before unmount');
    });
});
