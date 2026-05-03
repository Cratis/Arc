# Use Observable Queries with cURL

This guide shows you how to work with observable query endpoints by using plain HTTP tools such as `curl`.

Use this when you want to:

- confirm the current snapshot for an observable query
- wait for the first payload before the request returns
- follow a live Server-Sent Events (SSE) stream
- emulate long polling with repeated HTTP requests

The same approach works for both **model-bound** and **controller-based** observable query endpoints.

## Prerequisites

- You know the observable query URL you want to call.
- Your application is running.
- The endpoint already returns an observable query result (`ISubject<T>`).

## Get the current snapshot

A normal `GET` request returns the current observable snapshot as JSON.

```bash
curl "https://localhost:5001/api/orders/observe-all"
```

Use this when the observable already has a current value and you only want the latest snapshot once.

## Wait for the first payload

If the observable does not have a current value yet, add `waitForFirstResult=true`.

```bash
curl "https://localhost:5001/api/orders/observe-all?waitForFirstResult=true"
```

The request stays open until the observable produces its first payload or the timeout expires.

### Override the timeout

The default timeout is 30 seconds. Override it with `waitForFirstResultTimeout`, expressed in seconds.

```bash
curl "https://localhost:5001/api/orders/observe-all?waitForFirstResult=true&waitForFirstResultTimeout=10"
```

If the timeout expires, Arc returns an HTTP timeout response with a JSON error payload.

## Stream updates over SSE

To keep the connection open and watch updates continuously, request the endpoint as Server-Sent Events.

```bash
curl --no-buffer \
  -H "Accept: text/event-stream" \
  "https://localhost:5001/api/orders/observe-all"
```

Each update is sent as an SSE `data:` frame that contains a serialized `QueryResult`.

Example output:

```text
data: {"isSuccess":true,"data":[{"id":"...","status":"ready"}],"changeSet":null}

data: {"isSuccess":true,"data":[{"id":"...","status":"shipped"}],"changeSet":null}
```

Use this when you want a live stream instead of a single JSON response.

## Emulate long polling

If you want repeated snapshot requests instead of a continuous stream, call the endpoint in a loop and wait for the first payload each time.

```bash
while true; do
  curl --silent \
    "https://localhost:5001/api/orders/observe-all?waitForFirstResult=true&waitForFirstResultTimeout=15"
  echo
done
```

This is effectively **long polling**:

- each request waits until data is available or the timeout expires
- the server returns a normal JSON payload
- the client immediately opens a new request

Use this when SSE is not convenient and you still want blocking snapshot requests from plain HTTP tooling.

## Pick the right mode

| Goal | Request style |
|---|---|
| Get the latest snapshot right now | `GET /query` |
| Wait until the first payload exists | `GET /query?waitForFirstResult=true` |
| Wait with a custom timeout | `GET /query?waitForFirstResult=true&waitForFirstResultTimeout=10` |
| Follow live updates continuously | `GET /query` with `Accept: text/event-stream` |
| Repeated blocking snapshot requests | Long-poll loop with `waitForFirstResult=true` |

## See also

- [Model-bound observable queries](./model-bound/observable-queries.md)
- [Controller-based observable queries](./controller-based/observable-queries.md)
- [Observable Query Demultiplexer](./observable-query-demultiplexer.md)
