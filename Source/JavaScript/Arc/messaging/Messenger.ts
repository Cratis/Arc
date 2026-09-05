// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { filter, Subject, Subscription } from 'rxjs';
import { IMessenger } from './IMessenger';
import { Message } from './Message';

/**
 * Represents an implementation of {@link IMessenger}.
 */
export class Messenger extends IMessenger {
    bubbleToParent = false;
    trickleDownToChildren = true;

    private readonly _messages = new Subject<Message>();
    private readonly _children = new Set<Messenger>();

    constructor(private readonly _parent?: Messenger) {
        super();
        this._parent?._children.add(this);
    }

    /**
     * Dispose this messenger and remove it from its parent.
     */
    dispose() {
        this._parent?._children.delete(this);
    }

    /** @inheritdoc */
    publish<TMessage extends object>(message: TMessage): void {
        this._publishFromSelf(message);
    }

    /** @inheritdoc */
    subscribe<TMessage extends object>(type: Constructor<TMessage>, callback: (message: TMessage) => void): Subscription {
        const observable = this._messages.pipe(filter(m => m.type === type));
        const subscription = observable.subscribe(m => callback(m.content as TMessage));
        return subscription;
    }

    private _publishFromSelf<TMessage extends object>(message: TMessage) {
        this._publishLocally(message);

        if (this.bubbleToParent && this._parent) {
            this._parent._publishFromChild(message, this);
        }

        if (this.trickleDownToChildren) {
            this._publishToChildren(message);
        }
    }

    private _publishFromParent<TMessage extends object>(message: TMessage) {
        this._publishLocally(message);

        if (this.trickleDownToChildren) {
            this._publishToChildren(message);
        }
    }

    private _publishFromChild<TMessage extends object>(message: TMessage, child: Messenger) {
        this._publishLocally(message);

        if (this.bubbleToParent && this._parent) {
            this._parent._publishFromChild(message, this);
        }

        if (this.trickleDownToChildren) {
            this._publishToChildren(message, child);
        }
    }

    private _publishLocally<TMessage extends object>(message: TMessage) {
        this._messages.next(new Message(message.constructor as Constructor, message));
    }

    private _publishToChildren<TMessage extends object>(message: TMessage, excludedChild?: Messenger) {
        this._children.forEach(child => {
            if (child === excludedChild) {
                return;
            }
            child._publishFromParent(message);
        });
    }
}
