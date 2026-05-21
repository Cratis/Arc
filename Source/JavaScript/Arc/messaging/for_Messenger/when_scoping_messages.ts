// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Messenger } from '../Messenger';

class MessageToSend {
    constructor(readonly something: string) { }
}

describe('when scoping messages', () => {
    let root: Messenger;
    let child: Messenger;
    let grandchild: Messenger;
    let rootMessages: MessageToSend[];
    let childMessages: MessageToSend[];
    let grandchildMessages: MessageToSend[];

    beforeEach(() => {
        root = new Messenger();
        child = new Messenger(root);
        grandchild = new Messenger(child);
        rootMessages = [];
        childMessages = [];
        grandchildMessages = [];

        root.subscribe(MessageToSend, message => rootMessages.push(message));
        child.subscribe(MessageToSend, message => childMessages.push(message));
        grandchild.subscribe(MessageToSend, message => grandchildMessages.push(message));
    });

    afterEach(() => {
        grandchild.dispose();
        child.dispose();
        root.dispose();
    });

    describe('and publishing from child with default options', () => {
        beforeEach(() => child.publish(new MessageToSend('forty two')));

        it('should publish locally', () => childMessages.length.should.equal(1));
        it('should not bubble to parent', () => rootMessages.length.should.equal(0));
        it('should trickle to child messengers', () => grandchildMessages.length.should.equal(1));
    });

    describe('and publishing from parent with default options', () => {
        beforeEach(() => root.publish(new MessageToSend('forty two')));

        it('should trickle recursively to child messengers', () => {
            childMessages.length.should.equal(1);
            grandchildMessages.length.should.equal(1);
        });
    });

    describe('and child enables bubbling to parent', () => {
        beforeEach(() => {
            child.bubbleToParent = true;
            child.publish(new MessageToSend('forty two'));
        });

        it('should bubble to parent', () => rootMessages.length.should.equal(1));
    });

    describe('and parent disables trickling to children', () => {
        beforeEach(() => {
            root.trickleDownToChildren = false;
            root.publish(new MessageToSend('forty two'));
        });

        it('should not forward to any child messengers', () => {
            childMessages.length.should.equal(0);
            grandchildMessages.length.should.equal(0);
        });
    });
});
