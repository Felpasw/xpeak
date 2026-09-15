import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
    appId: 'com.xpeak.app',
    appName: 'XPeak',
    webDir: 'out',
    android: {
        allowMixedContent: false,
    },
    server: {
        androidScheme: 'https',
    },
};

export default config;
