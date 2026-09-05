// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { MessengerScope } from '../MessengerScope';
import { useMessenger } from '../useMessenger';
import { useSendMessage } from '../useSendMessage';

class MessageToSend {
    constructor(readonly content: string) {
    }
}

describe('when sending from within scope', () => {
    let rootMessenger: IMessenger;
    let rootConfig: ArcConfiguration;
    let receivedAtRoot: MessageToSend[];
    let receivedAtScope: MessageToSend[];

    beforeEach(() => {
        rootMessenger = new Messenger();
        rootConfig = {
            microservice: 'test-microservice',
            messenger: rootMessenger
        };
        receivedAtRoot = [];
        receivedAtScope = [];

        rootMessenger.subscribe(MessageToSend, message => receivedAtRoot.push(message));

        const ScopedSubscriber = () => {
            const messenger = useMessenger();
            messenger.subscribe(MessageToSend, message => receivedAtScope.push(message));
            const send = useSendMessage();
            send(new MessageToSend('scoped'));
            return null;
        };

        render(
            <ArcContext.Provider value={rootConfig}>
                <MessengerScope>
                    <ScopedSubscriber />
                </MessengerScope>
            </ArcContext.Provider>
        );
    });

    it('should publish to the scope messenger', () => {
        receivedAtScope.length.should.equal(1);
    });

    it('should not bubble to the root messenger by default', () => {
        receivedAtRoot.length.should.equal(0);
    });
});
