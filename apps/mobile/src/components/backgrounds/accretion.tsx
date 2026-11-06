import { Backgrounds } from '@xpeak/shared';

import EtchedAccretion from './etched-accretion';

import type { BackgroundManifest, BackgroundProps } from './types';

function EtchedAccretionDefault({ className }: BackgroundProps) {
    return (
        <div className={className}>
            <EtchedAccretion
                preset="crimson"
                height="100%"
                interactive={false}
                className="h-full w-full"
            />
        </div>
    );
}

export const etchedAccretionManifest: BackgroundManifest = {
    slug: Backgrounds.EtchedAccretion,
    name: 'Buraco negro',
    description: 'Shader WebGL de um disco de acreção em tom crimson.',
    Component: EtchedAccretionDefault,
};
