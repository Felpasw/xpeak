import { lightningManifest } from './lightning';

import type { BackgroundManifest } from './types';

/**
 * Registry of every background the app knows about. Keyed by `slug`
 * so the profile page (Phase 10 onward) can look up the user's
 * `active_background_slug` and render the matching component.
 *
 * ## Adicionando um wallpaper novo
 *
 * 1. Coloca o arquivo em `wallpapers/<slug>.jpg` (ou `.png`, `.webp`).
 * 2. Cria um arquivo `wallpaper-<slug>.tsx` aqui na pasta com um
 *    componente que faça `<img src>` ou `next/image` da imagem e
 *    exporta um manifest com `slug`, `name`, `description` opcional
 *    e `preview` apontando pra thumb.
 * 3. Adiciona o import + entry no map abaixo.
 *
 * Pra shaders animados (tipo `lightning.tsx`), mesmo fluxo — cria
 * o componente + manifest e registra aqui.
 */
export const BACKGROUNDS: Record<string, BackgroundManifest> = {
    [lightningManifest.slug]: lightningManifest,
};

export const DEFAULT_BACKGROUND_SLUG = lightningManifest.slug;

export function getBackground(slug: string | null | undefined): BackgroundManifest {
    if (slug && slug in BACKGROUNDS) {
        return BACKGROUNDS[slug]!;
    }
    return BACKGROUNDS[DEFAULT_BACKGROUND_SLUG]!;
}

export function listBackgrounds(): BackgroundManifest[] {
    return Object.values(BACKGROUNDS);
}
