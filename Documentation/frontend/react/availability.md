---
title: Cratis Arc React availability for navigation and screens
description: Hide navigation, screens and parts of your React app that are unavailable to the current tenant or user, and show them again live without a reload when that changes.
---

Availability decides whether a part of the application exists for the current tenant or user. Where [identity](identity.md) and `RequireRole` decide what a user may do, `AvailabilityProvider` and `RequireAvailable` decide what is offered at all, and update without a reload. They read the [availability query](../../backend/csharp/availability.md#read-availability-on-the-client) of the server, which reports what is not available now and again each time that changes.

The server enforces availability regardless. These components only stop the client offering what it would refuse.

## Provide availability

Wrap the application below the `Arc` provider. A host that does not use availability does not mount the provider, and then everything is available.

```tsx
import { Arc } from '@cratis/arc.react';
import { AvailabilityProvider } from '@cratis/arc.react/availability';

export const App = () => (
    <Arc microservice="fleet">
        <AvailabilityProvider>
            <Routes />
        </AvailabilityProvider>
    </Arc>
);
```

Until the first decision arrives nothing is reported as available, so a part is never shown before the client has been told it may be. If a later update fails, the last decision stays in force: a failure to decide is not a denial.

## Hide a screen or a part

`RequireAvailable` shows its children only while the part is available. A part is either the full name of a command or read model on the server, or a generated proxy: a proxy is matched by the slice it belongs to.

```tsx
import { RequireAvailable } from '@cratis/arc.react/availability';
import { QualityScores } from './QualityScores';

export const DataQualityScreen = () => (
    <RequireAvailable part={QualityScores} whileLoading={<Spinner />} unavailable={<NotAvailable />}>
        <QualityScoresTable />
    </RequireAvailable>
);
```

`Spinner` and `NotAvailable` stand for your application's UI. Both slots are optional and default to nothing.

## Filter navigation

`useAvailableItems` keeps the navigation entries whose part is available and updates when that changes. An entry without a part is always kept.

```tsx
import { AvailabilityPart } from '@cratis/arc/availability';
import { useAvailableItems } from '@cratis/arc.react/availability';
import { QualityScores } from './QualityScores';

interface Entry {
    title: string;
    path: string;
    part?: AvailabilityPart;
}

const entries: Entry[] = [
    { title: 'Home', path: '/' },
    { title: 'Data quality', path: '/quality', part: QualityScores }
];
const partOf = (entry: Entry) => entry.part;

export const Navigation = () => {
    const available = useAvailableItems(entries, partOf);
    return <nav>{available.map(entry => <a key={entry.path} href={entry.path}>{entry.title}</a>)}</nav>;
};
```

Declare the entries and `partOf` outside the component, or memoize them, so the filtered list is not recomputed on every render.

Use `useAvailability()` for a decision in code. It returns an `Availability` with `isAvailable(part)` and `filter(items, partOf)`.

## Read the reason on a result

A command or query result tells "not available for you" apart from "not allowed":

```tsx
import { isNotAvailable } from '@cratis/arc/availability';

const result = await command.execute();
if (result.isNotAvailable) {
    // The part is not available to this tenant or user.
}
if (result.isRetryable) {
    // The server could not decide. The same request can succeed when retried.
}

const queryResult = await query.perform();
if (isNotAvailable(queryResult)) {
    // The part is not available.
}
```

`CommandResult` has `isNotAvailable`, `onNotAvailable(callback)` and `isRetryable`. Query results carry `authorizationFailureReason` and `isRetryable`; use `isNotAvailable(result)` to read them. A not available result is also unauthorized, so `onUnauthorized` still runs.
