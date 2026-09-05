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

describe('when filter rejects message', () => {
    let rootMessenger: IMessenger;
    let rootConfig: ArcConfiguration;
    let received: MessageToSend[];
    let filteredArguments: MessageToSend[];

    beforeEach(() => {
        rootMessenger = new Messenger();
        rootConfig = {
            microservice: 'test-microservice',
            messenger: rootMessenger
        };
        received = [];
        filteredArguments = [];

        const Subscriber = () => {
            useOnMessage(
                MessageToSend,
                message => received.push(message),
                message => {
                    filteredArguments.push(message);
                    return message.content === 'wanted';
                }
            );
            return null;
        };

        render(
            <ArcContext.Provider value={rootConfig}>
                <Subscriber />
            </ArcContext.Provider>
        );

        rootMessenger.publish(new MessageToSend('unwanted'));
        rootMessenger.publish(new MessageToSend('wanted'));
    });

    it('should call filter for every published message of the type', () => {
        filteredArguments.length.should.equal(2);
    });

    it('should pass the typed message into the filter', () => {
        filteredArguments[0].content.should.equal('unwanted');
        filteredArguments[1].content.should.equal('wanted');
    });

    it('should only invoke callback for messages the filter accepts', () => {
        received.length.should.equal(1);
        received[0].content.should.equal('wanted');
    });
});
