'use client';

import { Sparkles } from 'lucide-react';
import { AnimatePresence, motion } from 'motion/react';

interface LevelUpOverlayProps {
    open: boolean;
    level: number;
    onDismiss: () => void;
}

const HEADLINE = 'Subiu de nível!';
const HINT = 'Toque em qualquer lugar pra continuar';
const BODY_PREFIX = 'Agora você é nível';

export function LevelUpOverlay({ open, level, onDismiss }: LevelUpOverlayProps) {
    return (
        <AnimatePresence>
            {open ? (
                <motion.button
                    type="button"
                    role="dialog"
                    aria-label={HEADLINE}
                    onClick={onDismiss}
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    className="fixed inset-0 z-50 flex flex-col items-center justify-center gap-4 bg-zinc-950/85 backdrop-blur-md"
                >
                    <motion.div
                        initial={{ scale: 0.7, rotate: -8 }}
                        animate={{ scale: 1, rotate: 0 }}
                        transition={{ type: 'spring', stiffness: 260, damping: 18 }}
                        className="flex h-24 w-24 items-center justify-center rounded-full border-2 border-sky-400/60 bg-sky-500/20 text-sky-200 shadow-[0_20px_60px_-15px_rgba(56,189,248,0.7)]"
                    >
                        <Sparkles className="h-12 w-12" strokeWidth={2} />
                    </motion.div>
                    <motion.div
                        initial={{ opacity: 0, y: 12 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ delay: 0.15 }}
                        className="flex flex-col items-center gap-1 text-center"
                    >
                        <p className="font-mono text-xs uppercase tracking-[0.35em] text-sky-300/80">
                            {HEADLINE}
                        </p>
                        <p className="text-3xl font-bold text-zinc-100">
                            {BODY_PREFIX} {level}
                        </p>
                    </motion.div>
                    <p className="mt-6 text-xs uppercase tracking-[0.25em] text-white/40">
                        {HINT}
                    </p>
                </motion.button>
            ) : null}
        </AnimatePresence>
    );
}
