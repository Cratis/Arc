// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Messenger } from '../Messenger.js';

class MessageToSend {
    constructor(readonly something: string) { }
}

describe('when publishing message with subscriber', () => {
    let messenger: Messenger;
    let message: MessageToSend;
    let callbackCalled = false;

    beforeEach(() => {
        messenger = new Messenger();
        messenger.subscribe(MessageToSend, (m: MessageToSend) => {
            callbackCalled = true;
            message = m;
        });
        messenger.publish(new MessageToSend('forty two'));
    });

    it('should call the callback', () => callbackCalled.should.be.true);
    it('should pass the message to the callback', () => message.should.not.be.undefined);
    it('should pass the correct content to the callback', () => message.something.should.equal('forty two'));
});