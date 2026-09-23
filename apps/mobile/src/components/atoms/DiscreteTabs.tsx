'use client';

import type { LucideIcon } from 'lucide-react';
import { motion } from 'motion/react';
import { useState } from 'react';

import { cn } from '@/lib/utils';

export interface DiscreteTab {
    id: string;
    title: string;
    Icon: LucideIcon;
}

interface DiscreteTabsProps {
    tabs: DiscreteTab[];
    activeId: string;
    onChange: (id: string) => void;
    className?: string;
}

const LAYOUT_SPRING = {
    type: 'spring' as const,
    damping: 20,
    stiffness: 230,
    mass: 1.2,
};

export function DiscreteTabs({
    tabs,
    activeId,
    onChange,
    className,
}: DiscreteTabsProps) {
    return (
        <div className={cn('flex items-center justify-center gap-3', className)}>
            {tabs.map((tab) => (
                <TabButton
                    key={tab.id}
                    tab={tab}
                    isActive={activeId === tab.id}
                    onSelect={() => onChange(tab.id)}
                />
            ))}
        </div>
    );
}

interface TabButtonProps {
    tab: DiscreteTab;
    isActive: boolean;
    onSelect: () => void;
}

function TabButton({ tab, isActive, onSelect }: TabButtonProps) {
    const [isLoaded, setIsLoaded] = useState(false);
    const { Icon } = tab;

    return (
        <motion.button
            type="button"
            layoutId={`discrete-tab-${tab.id}`}
            transition={{ layout: LAYOUT_SPRING }}
            onClick={() => {
                onSelect();
                setIsLoaded(true);
            }}
            aria-pressed={isActive}
            aria-label={tab.title}
            className="flex h-fit w-fit cursor-pointer"
            style={{ willChange: 'transform' }}
        >
            <motion.div
                layout
                transition={{ layout: LAYOUT_SPRING }}
                data-active={isActive}
                className={cn(
                    'flex items-center gap-1.5 overflow-hidden rounded-full border border-zinc-800 bg-zinc-900/70 p-3 font-mono uppercase text-zinc-500 shadow-md backdrop-blur transition-colors duration-150 ease-out',
                    'data-[active=true]:border-zinc-700 data-[active=true]:px-4 data-[active=true]:text-zinc-100',
                )}
            >
                <motion.div
                    layoutId={`discrete-tab-icon-${tab.id}`}
                    className="shrink-0"
                    style={{ willChange: 'transform' }}
                >
                    <Icon size={22} />
                </motion.div>
                {isActive ? (
                    <motion.div
                        className="flex items-center"
                        initial={isLoaded ? { opacity: 0, filter: 'blur(4px)' } : false}
                        animate={{ opacity: 1, filter: 'blur(0px)' }}
                        transition={{
                            duration: isLoaded ? 0.2 : 0,
                            ease: [0.86, 0, 0.07, 1],
                        }}
                    >
                        <motion.span
                            layoutId={`discrete-tab-text-${tab.id}`}
                            className="relative inline-block whitespace-nowrap font-mono text-sm font-medium uppercase"
                            style={{ willChange: 'transform' }}
                        >
                            {tab.title}
                        </motion.span>
                    </motion.div>
                ) : null}
            </motion.div>
        </motion.button>
    );
}
