import { Backgrounds } from '@xpeak/shared';

import { etchedAccretionManifest } from './accretion';
import { lightningManifest } from './lightning';
import { starsManifest } from './stars';

import type { BackgroundManifest } from './types';

/**
 * Registry of every background the app knows about. Keyed by the
 * shared `Backgrounds` enum so the profile picker and layout can
 * resolve the user's `background` column into a component.
 *
 * ## Adicionando um background novo
 *
 * 1. Cria um arquivo `<slug>.tsx` aqui exportando um componente
 *    puro (`h-full w-full`, sem eventos próprios) + um manifest
 *    tipado `BackgroundManifest`.
 * 2. Adiciona o membro em `Backgrounds` no `@xpeak/shared` e
 *    atualiza o enum C# (`Backgrounds`) + migration que roda
 *    `ALTER TYPE backgrounds ADD VALUE`.
 * 3. Importa o manifest e registra em `BACKGROUNDS`.
 */
export const BACKGROUNDS: Record<Backgrounds, BackgroundManifest> = {
    [Backgrounds.Stars]: starsManifest,
    [Backgrounds.Lightning]: lightningManifest,
    [Backgrounds.EtchedAccretion]: etchedAccretionManifest,
};

export const DEFAULT_BACKGROUND: Backgrounds = Backgrounds.Stars;

export function getBackground(
    slug: Backgrounds | null | undefined,
): BackgroundManifest {
    if (slug && slug in BACKGROUNDS) {
        return BACKGROUNDS[slug];
    }
    return BACKGROUNDS[DEFAULT_BACKGROUND];
}

export function listBackgrounds(): BackgroundManifest[] {
    return Object.values(BACKGROUNDS);
}
