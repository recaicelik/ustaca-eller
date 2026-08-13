# Unity project

Unity Hub and **Unity 6 LTS 6000.0.81f1** (iOS + Android modules) are installed.
The project skeleton here — package manifest, assembly definitions, editor
bootstrap — is authored by hand and pinned to that editor version.

> **One step is blocked and it needs you.** The editor refuses to open without a
> licence, and activating one means signing in with a Unity account:
>
> ```
> [Licensing::Client] Error: Code 404 (Found 0 entitlement groups and 0 free entitlements)
> ```
>
> Open Unity Hub, sign in, and activate a **Personal** licence — enough while annual
> revenue plus funding stays under $200,000. Everything below happens on first open.

## First open

1. Unity Hub → **Add project from disk** → select this `unity/` directory.
2. Unity resolves packages and generates `ProjectSettings/`, `Library/` and the
   `.meta` files. Expect a few minutes.
3. Run **Ustaca Eller → Apply project settings** from the menu bar.

That menu item comes from [Assets/Editor/ProjectBootstrap.cs](Assets/Editor/ProjectBootstrap.cs)
and applies every setting in the table below in one go, then audits the result. It
also re-audits on every editor load, so a setting someone flips back gets reported
immediately rather than at the next pull request.

> **Not yet compiled.** `ProjectBootstrap.cs` was written without a working editor,
> so its API calls are unverified. Treat the first open as its first test.

## Settings the bootstrap applies

| Setting | Value | Why |
|---|---|---|
| Splash screen and Unity logo | **off** | Unity 6 allows this on Personal; the Unity logo is the wrong first frame for a premium kids brand |
| Managed stripping level | **High** | Build size |
| Strip engine code | **on** | Build size |
| Api compatibility | **.NET Standard** | Build size |
| Scripting backend | **IL2CPP** | Required for iOS, and faster on the reference device |
| Android architecture | **ARM64** | Play Store requirement |
| Android min SDK | **24** | Covers the entry-level device base |
| Orientation | **Landscape** | A digital toy is held in two hands |
| `submitAnalytics` | **0** | Kids Category — no hardware statistics submission |

## Rules the bootstrap enforces

| Rule | Why |
|---|---|
| Never install `com.unity.services.*`, `com.unity.purchasing`, `com.unity.ads` | Documented Apple Kids Category rejection causes |
| Payments through the **RevenueCat Unity SDK** (current version) | Unity IAP cannot be enabled without Analytics |
| No literal user-facing strings in C# — the runtime reads `content/i18n` | A new language must cost content work only |

The same rules run in CI:

```bash
npm run compliance
```

## Packages

[Packages/manifest.json](Packages/manifest.json) is curated rather than generated.
Versions come from the 2D cross-platform template shipped inside this editor build,
so they are known-good for 6000.0.81f1.

`com.ustacaeller.core` is a local package pointing at [../core/UstacaEller.Core](../core/UstacaEller.Core) —
the same sources the .NET test suite compiles. One set of sources, two build paths.

Unity's own Localization package is deliberately absent: localization is content, and
it lives in `content/i18n` with the lookup logic in the core assembly.

## Phase 0 exit criteria

The project is not "ready" until both hold:

1. **Performance:** on the reference device (entry-level Android), 180 sprites plus
   4 skeletons with the cut mechanic active, at ≤ 16.7 ms frame time at the 95th
   percentile.
2. **Compliance:** a build with no `com.unity.services.*` and RevenueCat integrated,
   accepted on TestFlight under a "Made for Kids / ages 5 and under" declaration.

Front-loading the second one matters. Kids Category rejections usually surface at
the first real submission, and changing the payment architecture at that point is
expensive. Learning it in week three with an empty project is nearly free.

## Expected structure

```
unity/
  Assets/
    Scripts/
      Shell/          Scene selection, parent zone, paywall
      SceneRuntime/   Manifest reader and scene builder
      Mechanics/      Glue, Paint, Cut, Build — each independent
      Services/       Save, audio, entitlement, localization, anonymous telemetry
    Content/          Addressable groups, built from content/
  Packages/
  ProjectSettings/
```

Scene data is **not** stored here. It lives in `content/scenes/` and is compiled into
Addressable groups from there, so a scene author never has to open Unity. The same
applies to localization: `content/i18n` is the source, and voice-over ships as
`audio/<locale>/` inside the scene's Addressable group.
