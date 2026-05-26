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

describe('when publishing matching message', () => {
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

        rootMessenger.publish(new MessageToSend('forty two'));
    });

    it('should invoke callback with the message', () => {
        received.length.should.equal(1);
    });

    it('should pass the published message content through', () => {
        received[0].content.should.equal('forty two');
    });
});
