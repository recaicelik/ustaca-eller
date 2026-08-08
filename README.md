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
docs/             Market research, competitor analysis, tech choice, MVP roadmap
tools/            CI gates: scene validator, i18n validator, compliance check
unity/            Unity project (not created yet — see unity/README.md)
```

## Commands

No dependencies; Node 18+ is all you need.

```bash
npm run validate
```

```bash
npm run compliance
```

```bash
npm test
```

`validate` covers scene manifests and localization catalogs. `test` verifies the
gates themselves still catch what they claim to.

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
