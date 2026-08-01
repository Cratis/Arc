// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents a function that creates an {@link EventSource} for a given URL.
 *
 * Arc uses the global {@link EventSource} constructor by default. Provide a factory to
 * substitute a custom implementation — for example a native SSE client on React Native,
 * where {@link EventSource} is unavailable and JS-based polyfills built on
 * `XMLHttpRequest` have unreliable streaming behavior on Android.
 */
export type EventSourceFactory = (url: string) => EventSource;
