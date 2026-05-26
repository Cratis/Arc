// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { act, render } from '@testing-library/react';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { useSendMessage } from '../useSendMessage';

describe('when messenger is unchanged across renders', () => {
    let rootMessenger: IMessenger;
    let rootConfig: ArcConfiguration;
    let captured: Array<(message: object) => void>;
    let triggerRerender: () => void;

    beforeEach(() => {
        rootMessenger = new Messenger();
        rootConfig = {
            microservice: 'test-microservice',
            messenger: rootMessenger
        };
        captured = [];

        const Sender = () => {
            const [, setVersion] = useState(0);
            triggerRerender = () => setVersion(v => v + 1);
            const send = useSendMessage();
            captured.push(send);
            return null;
        };

        render(
            <ArcContext.Provider value={rootConfig}>
                <Sender />
            </ArcContext.Provider>
        );

        act(() => triggerRerender());
    });

    it('should return the same function reference across renders', () => {
        captured.length.should.be.greaterThan(1);
        captured[0].should.equal(captured[captured.length - 1]);
    });
});
