'use client';

import { useId } from 'react';

interface LiquidWordmarkProps {
    text?: string;
    width?: number;
    className?: string;
    align?: 'middle' | 'start';
}

const prefersReducedMotion = (): boolean =>
    typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;

export function LiquidWordmark({
    text = 'XPeak',
    width = 300,
    className,
    align = 'middle',
}: LiquidWordmarkProps) {
    const uid = useId().replace(/:/g, '');
    const gradId = `lw-grad-${uid}`;
    const filterId = `lw-water-${uid}`;
    const animate = !prefersReducedMotion();

    const viewBox = align === 'start' ? '0 0 240 108' : '0 0 400 108';
    const textX = align === 'start' ? 0 : 200;
    const textAnchor = align === 'start' ? 'start' : 'middle';

    return (
        <svg
            viewBox={viewBox}
            role="img"
            aria-label={text}
            className={className}
            style={{
                display: 'block',
                width,
                height: 'auto',
                overflow: 'visible',
                filter: 'drop-shadow(0 10px 30px rgba(255,255,255,0.28))',
            }}
        >
            <defs>
                <linearGradient
                    id={gradId}
                    gradientUnits="userSpaceOnUse"
                    x1="0"
                    y1="8"
                    x2="0"
                    y2="100"
                >
                    <stop offset="0" stopColor="#ffffff" />
                    <stop offset="0.32" stopColor="#f5f5f5" />
                    <stop offset="0.62" stopColor="#e5e5e5" />
                    <stop offset="1" stopColor="#d4d4d4" />
                    {animate && (
                        <animateTransform
                            attributeName="gradientTransform"
                            type="translate"
                            dur="5.5s"
                            repeatCount="indefinite"
                            calcMode="spline"
                            values="0 -10; 0 10; 0 -10"
                            keyTimes="0; 0.5; 1"
                            keySplines="0.45 0 0.55 1; 0.45 0 0.55 1"
                        />
                    )}
                </linearGradient>
                <filter
                    id={filterId}
                    x="-18%"
                    y="-30%"
                    width="136%"
                    height="160%"
                >
                    <feTurbulence
                        type="fractalNoise"
                        baseFrequency="0.011 0.028"
                        numOctaves={2}
                        seed={7}
                        result="n"
                    >
                        {animate && (
                            <animate
                                attributeName="baseFrequency"
                                dur="14s"
                                repeatCount="indefinite"
                                values="0.011 0.028; 0.016 0.020; 0.011 0.028"
                            />
                        )}
                    </feTurbulence>
                    <feDisplacementMap
                        in="SourceGraphic"
                        in2="n"
                        scale={6}
                        xChannelSelector="R"
                        yChannelSelector="G"
                    >
                        {animate && (
                            <animate
                                attributeName="scale"
                                dur="3.6s"
                                repeatCount="indefinite"
                                calcMode="spline"
                                values="6; 16; 6"
                                keyTimes="0; 0.5; 1"
                                keySplines="0.45 0 0.55 1; 0.45 0 0.55 1"
                            />
                        )}
                    </feDisplacementMap>
                </filter>
            </defs>
            <text
                x={textX}
                y="80"
                textAnchor={textAnchor}
                filter={`url(#${filterId})`}
                fill={`url(#${gradId})`}
                stroke="rgba(255,255,255,0.28)"
                strokeWidth={0.7}
                style={{
                    fontFamily: '"Plus Jakarta Sans", system-ui, sans-serif',
                    fontWeight: 800,
                    fontSize: 78,
                    letterSpacing: '-0.03em',
                    paintOrder: 'stroke fill',
                }}
            >
                {text}
            </text>
        </svg>
    );
}
