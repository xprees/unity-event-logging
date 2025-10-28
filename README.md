# Unity Event Logging Package - cz.xprees.event-logging

[![NPM Version](https://img.shields.io/npm/v/cz.xprees.event-logging)](https://www.npmjs.com/package/cz.xprees.event-logging)

This package provides a solution for logging events in a structured way. Which can be used to track user actions, system events, and other significant
occurrences in your application.

That can be used for Process Mining, Analytics, and Monitoring. This package is designed to work seamlessly with
the [CF-Bucket - Log collector service](https://github.com/cyber-framework/cf-bucket)

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
