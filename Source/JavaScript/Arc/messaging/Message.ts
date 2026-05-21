// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';

/**
 * Represents a message published on the {@link Messenger}.
 */
export class Message {

    /**
     * Initializes a new instance of {@link Message}.
     * @param {Constructor} type Type of the content in the message.
     * @param {*} content The actual content.
     */
    constructor(readonly type: Constructor, readonly content: object) {
    }
}
