// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { act, render } from '@testing-library/react';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { useOnMessage } from '../useOnMessage';

class MessageToSend {
    constructor(readonly content: string) {
    }
}

describe('when callback changes between renders', () => {
    let rootMessenger: IMessenger;
    let rootConfig: ArcConfiguration;
    let firstReceived: MessageToSend[];
    let secondReceived: MessageToSend[];
    let triggerRerender: () => void;

    beforeEach(() => {
        rootMessenger = new Messenger();
        rootConfig = {
            microservice: 'test-microservice',
            messenger: rootMessenger
        };
        firstReceived = [];
        secondReceived = [];

        const Subscriber = () => {
            const [version, setVersion] = useState(0);
            triggerRerender = () => setVersion(v => v + 1);
            const callback = version === 0
                ? (message: MessageToSend) => firstReceived.push(message)
                : (message: MessageToSend) => secondReceived.push(message);
            useOnMessage(MessageToSend, callback);
            return null;
        };

        render(
            <ArcContext.Provider value={rootConfig}>
                <Subscriber />
            </ArcContext.Provider>
        );

        rootMessenger.publish(new MessageToSend('first'));
        act(() => triggerRerender());
        rootMessenger.publish(new MessageToSend('second'));
    });

    it('should invoke the initial callback while it is current', () => {
        firstReceived.length.should.equal(1);
        firstReceived[0].content.should.equal('first');
    });

    it('should invoke the latest callback after a re-render without re-subscribing', () => {
        secondReceived.length.should.equal(1);
        secondReceived[0].content.should.equal('second');
    });
});
