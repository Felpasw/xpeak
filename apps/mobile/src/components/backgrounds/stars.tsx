import { Backgrounds } from '@xpeak/shared';

import { StarsBackground } from '@/components/atoms/StarsBackground';

import type { BackgroundManifest, BackgroundProps } from './types';

function StarsDefault({ className }: BackgroundProps) {
    return <StarsBackground className={className} starColor="#38bdf8" />;
}

export const starsManifest: BackgroundManifest = {
    slug: Backgrounds.Stars,
    name: 'Estrelas',
    description: 'Camadas de estrelas em parallax sobre gradiente radial.',
    Component: StarsDefault,
};
