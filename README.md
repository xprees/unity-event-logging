# Unity Event Logging Package - cz.xprees.event-logging

[![NPM Version](https://img.shields.io/npm/v/cz.xprees.event-logging)](https://www.npmjs.com/package/cz.xprees.event-logging)

This package provides a solution for logging events in a structured way. Which can be used to track user actions, system events, and other significant
occurrences in your application.

That can be used for Process Mining, Analytics, and Monitoring. This package is designed to work seamlessly with
the [CF-Bucket - Log collector service](https://github.com/cyber-framework/cf-bucket)

## Installation

Install the package using npm scoped registry in `Project Settings > Package Manager > Scoped Registries`

```json
{
    "name": "NPM - xprees",
    "url": "https://registry.npmjs.org",
    "scopes": [
        "cz.xprees"
    ]
}

```

Then simply install the package using the Unity Package Manager using the _NPM - xprees_ scope or by the package name `cz.xprees.event-logging`.

### Post-Installation

The package will automatically define the script define symbol `XPREES_EVENT_LOGGING` in the project settings. This is used to enable the event
logging functionality in your project and compatibility with other packages using event-logging capabilities.


