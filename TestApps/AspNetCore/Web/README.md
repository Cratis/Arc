# AspNetCore TestApp — Frontend

A Vite + React frontend that demonstrates the real-time query features built into this PR.

## Pages

| Page | What it demonstrates |
|---|---|
| **Ticker** | Subscribes to `Ticker.Observe()` via SSE. The counter increments every second on the server and is pushed to all connected clients in real time. |
| **Live Feed** | Demonstrates `LiveFeed.All()` (subscribe to all messages), `LiveFeed.ByAuthor(author)` (filtered subscription using `when(condition)`), and the `PostToFeed` command that triggers a real-time push to every subscriber. |

## Running locally

1. Start the backend (from `TestApps/AspNetCore`):
   ```sh
   dotnet run
   ```
   The backend listens on `http://localhost:5000`.

2. Start the frontend dev server (from `TestApps/AspNetCore/Web`):
   ```sh
   yarn dev
   ```
   Vite proxies `/api` and `/.cratis` requests to the backend, so SSE and command calls work without CORS issues.

3. Open `http://localhost:5173` in your browser.

## Frontend structure

```
Web/
├── index.html          ← HTML entry point
├── vite.config.ts      ← Vite config (proxies backend routes)
├── tsconfig.json       ← TypeScript config
├── package.json
└── src/
    ├── main.tsx        ← React entry point, mounts <Arc>
    ├── App.tsx         ← Root component with tab navigation
    ├── TickerPage.tsx  ← Live counter via Observe.use()
    └── LiveFeedPage.tsx← Live feed with PostToFeed command and ByAuthor filter
```

The generated TypeScript proxies live in `../Features/AspNetCore/` and are imported via the `Features/*` path alias.
