// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Wraps browser navigation actions that identity handling needs, behind a static surface a
 * specification can replace - `window.location` itself cannot be reassigned in most test
 * environments, and a real reload must never actually run while a specification is exercising it.
 */
export class BrowserNavigation {

    /**
     * Reloads the current page through an ordinary top-level navigation.
     */
    static reload(): void {
        window.location.reload();
    }
}
