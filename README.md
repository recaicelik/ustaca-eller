# Ustaca Eller

An open-ended digital toy for ages 2–6. Cutting, gluing, painting and building
mechanics that support fine motor skills. No ads, no in-app purchases; subscription
is the only revenue source.

**Engine:** Unity 6 (2D) · **Language:** C# · **Payments:** RevenueCat · **Platforms:** iOS + Android

> Code, comments, identifiers and tool output are English throughout.
> The research reports and roadmap in `docs/` are Turkish — they are a deliverable
> series with Word/PDF counterparts, not source.

---

## Layout

```
content/i18n/     Localization catalogs — one file per locale
content/scenes/   Scene manifests; scenes are data, not code
content/schema/   Scene manifest schema
core/             Game logic as a plain C# library, no engine dependency
docs/             Market research, competitor analysis, tech choice, MVP roadmap
tools/            CI gates: scene validator, i18n validator, compliance check
unity/            Unity project (not created yet — see unity/README.md)
```

## Commands

Node 18+ for the content pipeline, .NET 10 SDK for the game core.

```bash
npm run validate
```

```bash
npm run compliance
```

```bash
npm test
```

`validate` covers scene manifests and localization catalogs. `test` runs both the
gate tests and the game core tests (`npm run test:gates` / `npm run test:core` to
run one side alone) and takes seconds.

```bash
npm run test:unity
```

Runs the Unity EditMode tests headlessly. Kept out of `npm test` because it needs a
licensed Unity install and takes minutes — run it before pushing anything under
`unity/`.

```bash
npm run run:ios
```

Builds and launches on a booted iOS simulator. Boot one first, e.g.
`xcrun simctl boot "iPad Pro 11-inch (M5)"`. Needs the iOS platform installed
(`xcodebuild -downloadPlatform iOS`).

---

## Game core

`core/UstacaEller.Core` holds the parts of the game that are logic rather than
rendering: cutting geometry, snapping, grid placement and localization lookup.
It has **no UnityEngine references** and targets `netstandard2.1`.

That constraint buys three things:

1. **The hard part is testable in milliseconds.** Cutting a shape along a
   finger-drawn stroke is the riskiest mechanic in the product; verifying it needs
   arithmetic, not a GPU. `dotnet test` runs the whole suite in well under a second,
   on any machine, with no editor and no device.
2. **CI stays cheap.** Unity CI is slow and licensed; this is neither.
3. **The engine decision stays reversible.** Should Unity ever be swapped out — the
   technology report keeps Godot as a live second option — this assembly survives
   untouched.

Unity consumes the same files as a local package through
`Runtime/UstacaEller.Core.asmdef`; the `.csproj` exists so the sources also compile
without Unity. One set of sources, two build paths, no copies.

**Where the line sits:** anything that needs a transform, a sprite, a touch event or
a sound goes in the Unity layer. Anything that can be decided from numbers and ids
goes here.

## How content reaches the game

```
content/scenes/<id>/manifest.json      authored by hand, validated by npm run validate
  → Ustaca Eller → Sync content        copied into StreamingAssets
  → SceneCatalog                       read with Newtonsoft into SceneManifest
  → SceneBuilder                       GameObjects, positions, layer order
```

`content/` sits at the repository root rather than under `Assets/` so a scene author
edits JSON, runs the validator and never opens Unity. The sync step is the seam, and
it becomes an Addressables build once scenes ship as downloadable groups.

Art does not exist yet, so `SceneBuilder` renders every object as a flat coloured
quad — a greybox. Layout, layer order, drop zones and all four mechanics can be
exercised and measured now; the illustrator's work drops into the same slots later
without the runtime changing. Placeholder colours are derived from the object id, so
a prop keeps its colour between builds and screenshots stay comparable.

---

## Localization

The product is multi-language from day one. That decision shapes the content
pipeline more than it shapes the code, because **this age group reads almost
nothing** — the localized asset that matters is voice-over, not UI text.

**Three rules make a new language cost content work only, never engineering:**

1. **No literal text in manifests.** Scenes reference localization keys
   (`titleKey: "scene.kitchen.title"`). Embedding `{tr, en}` dictionaries in
   manifests would mean editing every scene for every new language.
2. **No text baked into art.** A sprite containing a word becomes a localized
   asset, which multiplies the art budget by the number of languages.
3. **Voice is a typed asset.** Audio declared as `type: "voice"` is resolved at
   runtime from `audio/<locale>/<file>`; `sfx` and `ambience` are shared. Putting
   an sfx in a voice slot is a validator error, because it would silently ship a
   Turkish clip to English users.

Locales are declared in [content/i18n/locales.json](content/i18n/locales.json).
A locale marked `shipping: true` must be fully translated or CI fails; others may
lag behind and fall back, but every gap is reported.

Adding a language: add it to `locales.json`, add `<code>.json`, record voice-over
into `audio/<code>/`. No code change.

## Adding a scene

Create `content/scenes/<scene_id>/manifest.json` and run `npm run validate`.

The schema is in [content/schema/scene.schema.json](content/schema/scene.schema.json);
the `$schema` field gives you editor autocomplete. Worked example with all four
mechanics: [content/scenes/kitchen/manifest.json](content/scenes/kitchen/manifest.json).

The validator checks structure, cross-references (undeclared layer, undefined
audio, wrong zone type), localization keys and the performance budget together.

---

## Non-negotiables

Each one is either a legal requirement or an architectural decision that is
expensive to reverse. Reasoning: [Technology and Architecture Report](docs/Ustaca_Eller_Teknoloji_ve_Mimari_Raporu.md) (Turkish).

1. **`com.unity.services.*` never enters the project.** Unity Analytics and Unity
   IAP are documented Apple Kids Category rejection causes. Payments go through
   RevenueCat. → enforced by `npm run compliance`.
2. **No child accounts.** No sign-up, email, name, date of birth, microphone or
   camera. A child's creations stay on the device. This is what keeps most of
   COPPA's verified-parental-consent burden from ever arising.
3. **No third-party analytics.** Telemetry is first-party, anonymous, aggregate.
4. **Purchases, outbound links and permission prompts sit behind a parental gate.**
5. **No dark patterns.** No comeback notifications, streaks, daily rewards, or
   emotional manipulation on exit.
6. **Scenes are data.** If adding a scene requires a C# change, the architecture
   has broken.
7. **Acceptance is measured on the reference device** — entry-level Android, not
   an iPhone.

## Documents

| Document | Contents |
|---|---|
| [MVP Roadmap](docs/MVP_Yol_Haritasi.md) | Phases, exit criteria, team, schedule |
| [Technology and Architecture Report](docs/Ustaca_Eller_Teknoloji_ve_Mimari_Raporu.md) | Unity/Godot/React Native comparison, architecture, compliance |
| [Competitor Analysis](docs/Ustaca_Eller_Rakip_Analizi.pdf) | Toca Boca, Sago Mini, PAW Patrol Academy, the Turkish market |
| [Market Research](docs/Cocuk_Gelisim_Uygulamalari_Arastirma_Raporu.pdf) | Market size, revenue models, regulation, product ideas |
