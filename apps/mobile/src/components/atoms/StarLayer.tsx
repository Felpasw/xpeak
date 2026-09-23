'use client';

import type { HTMLMotionProps, Transition } from 'motion/react';
import { motion } from 'motion/react';
import { useMemo, useSyncExternalStore } from 'react';

import { generateStars } from '@/components/atoms/utils/generateStars';
import { cn } from '@/lib/utils';

type StarLayerProps = HTMLMotionProps<'div'> & {
    count: number;
    size: number;
    transition: Transition;
    starColor: string;
};

const noopSubscribe = () => () => {};
const getIsClient = () => true;
const getIsServer = () => false;

function useIsClient() {
    return useSyncExternalStore(noopSubscribe, getIsClient, getIsServer);
}

export function StarLayer({
    count = 1000,
    size = 1,
    transition = { repeat: Infinity, duration: 50, ease: 'linear' },
    starColor = '#fff',
    className,
    ...props
}: StarLayerProps) {
    const isClient = useIsClient();
    const boxShadow = useMemo(
        () => (isClient ? generateStars(count, starColor) : ''),
        [isClient, count, starColor],
    );

    return (
        <motion.div
            data-slot="star-layer"
            animate={{ y: [0, -2000] }}
            transition={transition}
            className={cn('absolute top-0 left-0 w-full h-[2000px]', className)}
            {...props}
        >
            <div
                className="absolute bg-transparent rounded-full"
                style={{
                    width: `${size}px`,
                    height: `${size}px`,
                    boxShadow,
                }}
            />
            <div
                className="absolute bg-transparent rounded-full top-[2000px]"
                style={{
                    width: `${size}px`,
                    height: `${size}px`,
                    boxShadow,
                }}
            />
        </motion.div>
    );
}
