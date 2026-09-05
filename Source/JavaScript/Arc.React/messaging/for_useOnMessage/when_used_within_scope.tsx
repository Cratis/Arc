// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { MessengerScope } from '../MessengerScope';
import { useOnMessage } from '../useOnMessage';

class MessageToSend {
    constructor(readonly content: string) {
    }
}

describe('when used within scope', () => {
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
                <MessengerScope>
                    <Subscriber />
                </MessengerScope>
            </ArcContext.Provider>
        );

        rootMessenger.publish(new MessageToSend('from root'));
    });

    it('should receive messages trickled down from the root messenger', () => {
        received.length.should.equal(1);
        received[0].content.should.equal('from root');
    });
});
