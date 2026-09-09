# XPeak

Academia + RPG. Check-in dá XP, treino sobe nível, streak dá bônus.

## Stack

- Monorepo com pnpm workspaces
- Mobile: Capacitor (Android/iOS) empacotando app web
- Backend: a definir

## Estrutura

```
xpeak/
  apps/
    mobile/    # app Capacitor (frontend + wrapper nativo)
    api/       # backend (opcional, a decidir)
  packages/
    shared/    # tipos/constantes compartilhados
```

## Setup

```sh
pnpm install
pnpm dev
```
