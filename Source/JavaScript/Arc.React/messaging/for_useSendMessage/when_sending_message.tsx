// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { useSendMessage } from '../useSendMessage';

class MessageToSend {
    constructor(readonly content: string) {
    }
}

describe('when sending message', () => {
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

        rootMessenger.subscribe(MessageToSend, message => received.push(message));

        const Sender = () => {
            const send = useSendMessage();
            send(new MessageToSend('hello'));
            return null;
        };

        render(
            <ArcContext.Provider value={rootConfig}>
                <Sender />
            </ArcContext.Provider>
        );
    });

    it('should publish the message to the nearest messenger', () => {
        received.length.should.equal(1);
    });

    it('should pass the message content through unchanged', () => {
        received[0].content.should.equal('hello');
    });
});
