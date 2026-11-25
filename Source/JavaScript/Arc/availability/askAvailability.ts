// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQuerySubscription } from '../queries/ObservableQuerySubscription.js';
import { AvailabilityPart, partNameOf } from './Availability.js';
import { AvailableParts } from './AvailableParts.js';
import { ObserveAvailableParts } from './ObserveAvailableParts.js';
import { PartAvailability } from './PartAvailability.js';

/**
 * Describes the answer to a question about one part from the server's list of what is not available.
 * @param name The full name of the part that was asked about.
 * @param parts What the server listed.
 * @returns The {@link PartAvailability}.
 */
export function answerFor(name: string, parts: AvailableParts): PartAvailability {
    const listed = parts.unavailable.find(_ => _.name === name);
    return listed === undefined ? PartAvailability.available : PartAvailability.refused(listed.reason);
}

/**
 * Asks the server whether a part is available for the data it is about, for example one vessel.
 *
 * The server decides with the same gate it uses when the command or query runs, reading `arguments.name` from the
 * arguments given here, so the client never holds a rule of its own. It answers once; use `useIsAvailable` in React
 * to keep the answer current.
 * @param part The full name of a command or read model, or a generated proxy.
 * @param args The arguments of the command or query, by property name. Arguments are for deciding availability only;
 * they never grant anything.
 * @returns The {@link PartAvailability}. When the server could not decide, the answer is {@link PartAvailability.unknown},
 * which is not a denial and not a grant: ask again.
 * @throws When the proxy does not carry the part it is. Generate the proxies again.
 */
export function askAvailability(part: AvailabilityPart, args?: Record<string, unknown>): Promise<PartAvailability> {
    const name = partNameOf(part);
    if (name === undefined) {
        return Promise.reject(new Error('The proxy does not carry the part it is. Generate the proxies again with the current ProxyGenerator.'));
    }

    return new Promise<PartAvailability>(resolve => {
        const held: { subscription?: ObservableQuerySubscription<AvailableParts> } = {};
        let finished = false;
        const finish = (answer: PartAvailability) => {
            if (finished) {
                return;
            }

            finished = true;
            held.subscription?.unsubscribe();
            resolve(answer);
        };

        const query = new ObserveAvailableParts();
        held.subscription = query.subscribe(result => {
            if (result.isSuccess && result.isAuthorized && result.data?.unavailable !== undefined) {
                finish(answerFor(name, result.data));
            } else if (!result.isSuccess) {
                finish(PartAvailability.unknown);
            }
        }, { part: name, arguments: JSON.stringify(args ?? {}) });

        if (finished) {
            held.subscription.unsubscribe();
        }
    });
}
