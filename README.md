# Unity Event Logging Package - cz.xprees.event-logging

[![NPM Version](https://img.shields.io/npm/v/cz.xprees.event-logging)](https://www.npmjs.com/package/cz.xprees.event-logging)

This package provides a solution for logging events in a structured way. Which can be used to track user actions, system events, and other significant
occurrences in your application.

That can be used for Process Mining, Analytics, and Monitoring. This package is designed to work seamlessly with
the **Open Game Analytics** API (multitenant ingestion contract v1, see `Runtime/Api/OpenGameAnalytics/V1/og-analytics-v1.json`).
The old CF-Bucket client (`Api/Legacy`) is `[Obsolete]` and will be removed in the next major version. This backend has no `/logs/batch` or
`/logs/scenarios`, so builds using the old client keep logging to the old backend until they migrate.

## Installation

Install the package using npm scoped registry in `Project Settings > Package Manager > Scoped Registries` (For more details
see [Unity Docs - Use a scoped registry in your project](https://docs.unity3d.com/6000.2/Documentation/Manual/upm-scoped-use.html))

`Packages/manifest.json`

```json
{
    "scopedRegistries": [
        {
            "name": "package.openupm.com",
            "url": "https://package.openupm.com",
            "scopes": [
                "com.cysharp"
            ]
        },
        {
            "name": "NPM - xprees",
            "url": "https://registry.npmjs.org",
            "scopes": [
                "cz.xprees"
            ]
        }
    ]
}
```

Then simply install the package using the Unity Package Manager using the _NPM - xprees_ scope or by the package name `cz.xprees.event-logging`.

### Post-Installation

The package will automatically define the script define symbol `XPREES_EVENT_LOGGING` in the project settings. This is used to enable the event
logging functionality in your project and compatibility with other packages using event-logging capabilities.

## Quick Start

For a quick start, you can use the `EventLogManager` sample, that will give you a head start on how to log events in your project.
Import the sample via the Package Manager UI by selecting the package and clicking on the _Import Sample_ button.
Then you can use the `EventLogManager` component in your scene or just copy the script to your project and adjust to your need!

## Open Game Analytics v1 setup (2.0+)

1. Create the config asset in `Project Settings > Open Game Analytics` (default `Assets/Settings/OpenGameAnalytics/OGAnalyticsConfig.asset`) and fill
   in
   `Base URL`, `Org slug`, `App slug` and the publishable `Anon key` (all from the dashboard's key snippet).
   Keys always expire (max 1 year) - ship a new key before the old one expires.
2. Press **Test connection**. It sends one sample event (fixed id, so repeated runs are reported as duplicates) to the `connection-test` scenario as
   the
   `settings-test` player and tells you whether the key is invalid/revoked/expired, belongs to another app, the app is unknown or only the scenario is
   missing.
3. Send events with `OGAnalyticsV1Api.IngestEvents(scenarioSlug, events)` (one request per scenario, 1..500 events) - or start from the
   `EventLogManager` sample.
   The sample uses the global settings (`OGAnalyticsConfigSO.Instance`, registered in Preloaded Assets by the settings page) - no asset to assign; the
   optional `configOverride` field replaces them for one component. Sending options (`dontSendInEditor`, upload on quit/disable, debug logs) live in
   the config too.
   Uploads can also run on a schedule: `uploadIntervalSeconds` (default 30, 0 = off) and `uploadThreshold` (upload once N events are waiting, 0 =
   off).

Key rules of the contract the package takes care of (`Util/OGValidation`, `Util/Uuid7`, `Util/OGJsonWriter`):

- `playerId`: `A-Za-z0-9._:-`, 1-128 chars, unique per game app. It is the process-mining case id - persist it per install.
- `group` and scenario: slugs `a-z0-9._-`, max 100 chars. The group is per scenario and immutable once set.
- Event `id`: client generated UUIDv7 (idempotency key) - create it when the event happens, not when it is uploaded, so retries are deduplicated.
- `eventData` max 16 KiB.

Handle API errors by the ProblemDetails `code` (`OGApiResult.HasProblem`) - never by `title`:

| Response                                                            | Do                                                                                       |
|---------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| `202` with `groupErrors`                                            | Events are stored without the group - stop sending that group                            |
| `202` with `rejected` (e.g. `EventLog.TimestampInFuture`)           | Those events are dropped for good - don't resend them                                    |
| `400 Validation.Failed`                                             | Fix the data, retrying doesn't help                                                      |
| `401 ApiKey.Missing/Invalid/Revoked/Expired`, `403 ApiKey.WrongApp` | Stop sending, keep the queue, log it, ship a new key                                     |
| `404 Org.NotFound`, `GameApp.NotFound`                              | Stop sending - check base URL, org and app                                               |
| `404 Scenario.NotFound`                                             | The app forbids auto-creating scenarios - create it in the dashboard                     |
| `429 RateLimit.Exceeded`                                            | Wait `Retry-After` seconds (1..60). Limit: 100 requests / 60 s per key and IP by default |
| `5xx`, network                                                      | Retry with the same event ids (idempotent)                                               |

Keys are JWTs: the settings page reads `exp` and shows the expiry. A key only works against the server that issued it (a local API without
`ANON_KEY_SIGNING_KEY` issues new keys on every start). Old timestamps are accepted, only events more than 24 h in the future are rejected.
For WebGL builds the API allows any origin on the game endpoints and exposes `Retry-After`; call the API's domain.
