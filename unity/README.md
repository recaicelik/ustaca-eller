# Unity project — not created yet

This directory will hold the Unity 6 project. It could not be created on this
machine because **Unity Hub is not installed.** That is the first task of Phase 0.

## Setup

1. Install **Unity Hub**: https://unity.com/download
2. Install **Unity 6 LTS** through Hub, with these modules:
   - iOS Build Support
   - Android Build Support (+ OpenJDK, Android SDK & NDK Tools)
3. Licensing: **Personal** is enough while annual revenue plus funding stays under
   $200,000.
4. Create a new project here from the **2D (URP)** template, named `UstacaEller`.

## Day-one settings

These are cheap now and expensive later.

| Setting | Value | Why |
|---|---|---|
| Player → Splash Image → Show Unity Logo | **off** | Unity 6 allows this even on the free tier; matters for a premium kids brand |
| Player → Other → Managed Stripping Level | **High** | Build size |
| Player → Other → Strip Engine Code | **on** | Build size |
| Player → Other → Api Compatibility Level | **.NET Standard 2.1** | Build size |
| Project Settings → `submitAnalytics` | **0** | Kids Category — no hardware stats submission |
| Package Manager | never install `com.unity.services.*`, `com.unity.purchasing`, `com.unity.ads` | Documented Apple Kids Category rejection causes |
| Payments | **RevenueCat Unity SDK** (current version) | Unity IAP cannot be enabled without Analytics |
| Localization | Runtime reads `content/i18n`; **no literal user-facing strings in C#** | A new language must cost content work only |

Run the compliance gate once the project exists — it verifies most of the above:

```bash
npm run compliance
```

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
