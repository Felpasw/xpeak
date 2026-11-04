import { Backgrounds } from '@xpeak/shared';

import EtchedAccretion from './etched-accretion';

import type { BackgroundManifest, BackgroundProps } from './types';

function EtchedAccretionDefault({ className }: BackgroundProps) {
    return (
        <EtchedAccretion
            className={className}
            preset="crimson"
            height="100%"
            interactive={false}
        />
    );
}

export const etchedAccretionManifest: BackgroundManifest = {
    slug: Backgrounds.EtchedAccretion,
    name: 'Buraco negro',
    description: 'Shader WebGL de um disco de acreção em tom crimson.',
    Component: EtchedAccretionDefault,
};
