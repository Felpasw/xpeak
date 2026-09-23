import type { PropsWithChildren } from 'react';

import { BottomDock } from '@/components/atoms/BottomDock';

export default function AppLayout({ children }: PropsWithChildren) {
    return (
        <>
            {children}
            <BottomDock />
        </>
    );
}
