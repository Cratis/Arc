---
title: Cratis Arc MVVM browser services
description: Inject navigation and local storage into Cratis Arc view models, save browser preferences, and account for navigation callback and lifetime limits.
---

Cratis Arc provides `ILocalStorage` and `INavigation` so view models can save browser preferences or react to URL changes through injected dependencies. Use them after [setting up the MVVM context](./mvvm-context.md). The implementations still use browser APIs; injection lets you supply stand-ins when testing the view model separately.

## Available Interfaces

### INavigation

Inject `INavigation` when a view model needs notification of URL changes. It observes navigation; it has no method for navigating to another page. Check its callback and lifetime limits below before subscribing from a view that can unmount.

```typescript
export type UrlChangedCallback = (url: string, previousUrl: string) => void;

export abstract class INavigation {
    abstract onUrlChanged(callback: UrlChangedCallback): void;
}
```

#### Usage in a View Model

```typescript
import { injectable, inject } from 'tsyringe';
import { INavigation } from '@cratis/arc.react.mvvm/browser';

@injectable()
export class MyViewModel {
    constructor(@inject(INavigation) private readonly _navigation: INavigation) {
        this._navigation.onUrlChanged((url, previousUrl) => {
            console.log(`Navigated from ${previousUrl} to ${url}`);
            // Handle navigation change
        });
    }
}
```

The `Navigation` implementation watches DOM mutations with a `MutationObserver` and compares `location.href` with the URL captured when the service was created. That lets it notice client-side routing changes, such as React Router navigations, when they re-render the page.

:::caution[Current limits of INavigation]
- `previousUrl` is the URL at service creation, not the URL before the latest navigation.
- Because the comparison is against that initial URL, once the app has navigated away, every later DOM mutation calls your callback again with the same `url`. Make callbacks idempotent; the visited-URL example below records duplicates.
- A URL change that causes no DOM mutation is not reported.
- `onUrlChanged` returns no unsubscribe function and `Navigation` is a singleton, so a subscribed view model is retained for the life of the app.

When every navigation must be observed exactly once, use your router's location API instead.
:::

### ILocalStorage

Inject `ILocalStorage` to read and write browser storage without reaching for the global `localStorage` in the view model. It follows the standard `Storage` interface:

```typescript
export abstract class ILocalStorage implements Storage {
    abstract length: number;
    abstract clear(): void;
    abstract getItem(key: string): string | null;
    abstract key(index: number): string | null;
    abstract removeItem(key: string): void;
    abstract setItem(key: string, value: string): void;
}
```

#### Usage in a View Model

```typescript
import { injectable, inject } from 'tsyringe';
import { ILocalStorage } from '@cratis/arc.react.mvvm/browser';

@injectable()
export class PreferencesViewModel {
    constructor(@inject(ILocalStorage) private readonly _localStorage: ILocalStorage) {
    }

    savePreference(key: string, value: string): void {
        this._localStorage.setItem(key, value);
    }

    loadPreference(key: string): string | null {
        return this._localStorage.getItem(key);
    }

    clearAllPreferences(): void {
        this._localStorage.clear();
    }
}
```

`savePreference()` writes a string under the supplied key, and `loadPreference()` returns that string or `null`. The injected service is the native storage object, so storage restrictions and browser errors still apply. `clearAllPreferences()` calls `clear()`, which removes every local-storage entry for the origin, not only entries created by this view model. Use `removeItem(key)` when you only intend to remove one preference.

## Dependency Injection Setup

The [MVVM context](./mvvm-context.md) calls `Bindings.initialize()` for you. It registers `INavigation` as a singleton backed by `Navigation`, and `ILocalStorage` as the browser's native `localStorage` instance. Neither service is scoped to one mounted view, and neither needs a manual registration in that view.

## Benefits

Keeping browser access in constructor dependencies lets a view model test supply a storage stand-in or trigger a navigation callback without changing the real page. It also makes the dependency visible when reading the class. The abstraction does not change the underlying storage format or add navigation cleanup.

## Example: Complete View Model

The following class stores observed URLs under `visitedUrls` and restores them on construction. It illustrates both services, not an exact navigation history: repeated DOM mutations can append the same URL, and the navigation callback cannot be unsubscribed. Prefer the router's location API for a history tied to a mounted view.

The example also assumes the stored value is valid JSON containing an array. Validate or recover from stale browser data before using this storage pattern in your application.

```typescript
import { injectable, inject } from 'tsyringe';
import { INavigation, ILocalStorage } from '@cratis/arc.react.mvvm/browser';
import { makeObservable, observable } from 'mobx';

@injectable()
export class NavigationHistoryViewModel {
    @observable currentUrl: string = '';
    @observable visitedUrls: string[] = [];

    constructor(
        @inject(INavigation) private readonly _navigation: INavigation,
        @inject(ILocalStorage) private readonly _localStorage: ILocalStorage
    ) {
        makeObservable(this);
        
        // Load previously visited URLs from storage
        const stored = this._localStorage.getItem('visitedUrls');
        if (stored) {
            this.visitedUrls = JSON.parse(stored);
        }

        // Track URL changes
        this._navigation.onUrlChanged((url, previousUrl) => {
            this.currentUrl = url;
            this.visitedUrls.push(url);
            this._localStorage.setItem('visitedUrls', JSON.stringify(this.visitedUrls));
        });
    }

    clearHistory(): void {
        this.visitedUrls = [];
        this._localStorage.removeItem('visitedUrls');
    }
}
```
