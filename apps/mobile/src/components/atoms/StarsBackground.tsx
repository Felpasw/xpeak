'use client';

import type { SpringOptions } from 'motion/react';
import { motion, useMotionValue, useSpring } from 'motion/react';
import type { ComponentProps, MouseEvent } from 'react';
import { useCallback } from 'react';

import { StarLayer } from '@/components/atoms/StarLayer';
import { cn } from '@/lib/utils';

type StarsBackgroundProps = ComponentProps<'div'> & {
    factor?: number;
    speed?: number;
    transition?: SpringOptions;
    starColor?: string;
};

export function StarsBackground({
    children,
    className,
    factor = 0.05,
    speed = 50,
    transition = { stiffness: 50, damping: 20 },
    starColor = '#fff',
    ...props
}: StarsBackgroundProps) {
    const offsetX = useMotionValue(1);
    const offsetY = useMotionValue(1);

    const springX = useSpring(offsetX, transition);
    const springY = useSpring(offsetY, transition);

    const handleMouseMove = useCallback(
        (e: MouseEvent<HTMLDivElement>) => {
            const centerX = window.innerWidth / 2;
            const centerY = window.innerHeight / 2;
            const newOffsetX = -(e.clientX - centerX) * factor;
            const newOffsetY = -(e.clientY - centerY) * factor;
            offsetX.set(newOffsetX);
            offsetY.set(newOffsetY);
        },
        [offsetX, offsetY, factor],
    );

    return (
        <div
            data-slot="stars-background"
            className={cn(
                'relative size-full overflow-hidden bg-[radial-gradient(ellipse_at_bottom,_#262626_0%,_#000_100%)]',
                className,
            )}
            onMouseMove={handleMouseMove}
            {...props}
        >
            <motion.div style={{ x: springX, y: springY }}>
                <StarLayer
                    count={1000}
                    size={1}
                    transition={{ repeat: Infinity, duration: speed, ease: 'linear' }}
                    starColor={starColor}
                />
                <StarLayer
                    count={400}
                    size={2}
                    transition={{
                        repeat: Infinity,
                        duration: speed * 2,
                        ease: 'linear',
                    }}
                    starColor={starColor}
                />
                <StarLayer
                    count={200}
                    size={3}
                    transition={{
                        repeat: Infinity,
                        duration: speed * 3,
                        ease: 'linear',
                    }}
                    starColor={starColor}
                />
            </motion.div>
            {children}
        </div>
    );
}
