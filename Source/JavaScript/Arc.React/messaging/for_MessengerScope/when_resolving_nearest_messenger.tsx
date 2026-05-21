// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { MessengerScope } from '../MessengerScope';
import { useMessenger } from '../useMessenger';

class MessageToSend {
    constructor(readonly content: string) {
    }
}

describe('when resolving nearest messenger', () => {
    let rootMessenger: IMessenger;
    let rootConfig: ArcConfiguration;

    beforeEach(() => {
        rootMessenger = new Messenger();
        rootConfig = {
            microservice: 'test-microservice',
            messenger: rootMessenger
        };
    });

    it('should resolve Arc root messenger when no scope exists', () => {
        let resolved: IMessenger | undefined;

        render(
            <ArcContext.Provider value={rootConfig}>
                <CaptureMessenger onCaptured={messenger => resolved = messenger} />
            </ArcContext.Provider>
        );

        resolved!.should.equal(rootMessenger);
    });

    it('should resolve scoped messenger and receive parent messages by default', () => {
        let resolved: IMessenger | undefined;
        let messageFromRoot: MessageToSend | undefined;

        render(
            <ArcContext.Provider value={rootConfig}>
                <MessengerScope>
                    <CaptureMessenger
                        onCaptured={messenger => {
                            resolved = messenger;
                            messenger.subscribe(MessageToSend, message => messageFromRoot = message);
                        }} />
                </MessengerScope>
            </ArcContext.Provider>
        );

        resolved!.should.not.equal(rootMessenger);
        rootMessenger.publish(new MessageToSend('forty two'));
        if (!messageFromRoot) {
            throw new Error('Expected child scope to receive parent message');
        }
        messageFromRoot.content.should.equal('forty two');
    });
});

const CaptureMessenger = (props: { onCaptured: (messenger: IMessenger) => void; }) => {
    const messenger = useMessenger();
    props.onCaptured(messenger);
    return null;
};
