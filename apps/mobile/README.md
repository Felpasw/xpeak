# Xpeak Mobile

Next.js 16 (App Router) wrapped by Capacitor for Android/iOS.
Static export so the whole app can be bundled natively.

## Prerequisites

- Node 20+
- pnpm 9+ (installed via corepack or standalone)
- (Optional) Android Studio for the Android build
- (Optional) macOS + Xcode + CocoaPods for iOS

## Install (from repo root)

```sh
pnpm install
```

## Dev server (web preview)

```sh
pnpm --filter @xpeak/mobile dev
# http://localhost:3001
```

## Tests

```sh
pnpm --filter @xpeak/mobile test        # single run
pnpm --filter @xpeak/mobile test:watch  # watch mode
```

## Build the static bundle

```sh
pnpm --filter @xpeak/mobile build       # -> apps/mobile/out/
```

## Sync into native platforms

```sh
pnpm --filter @xpeak/mobile build
pnpm --filter @xpeak/mobile cap:sync
```

## Open native shells

```sh
pnpm --filter @xpeak/mobile cap:open:android   # Android Studio
pnpm --filter @xpeak/mobile cap:open:ios       # Xcode (macOS only)
```

## Env vars

Copy `.env.example` to `.env.local` and fill it in. `NEXT_PUBLIC_*`
values are inlined in the client bundle, so nothing sensitive there.

## Notes

- `next.config.ts` uses `output: 'export'` and `images.unoptimized: true`
  so the build produces a static bundle that Capacitor can wrap.
- iOS platform generation on Linux succeeds but skips `pod install`
  and Xcode steps; the `ios/` folder is still committed so a
  macOS dev can pick it up.
- Auth deep link (Phase 3) uses the `xpeak://` scheme registered in
  `capacitor.config.ts` and the native manifests.
