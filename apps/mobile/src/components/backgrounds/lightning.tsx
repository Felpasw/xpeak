'use client';

import { useEffect, useRef } from 'react';

import { cn } from '@/lib/utils';

import type { BackgroundManifest, BackgroundProps } from './types';

interface LightningTuning {
    hue?: number;
    xOffset?: number;
    speed?: number;
    intensity?: number;
    size?: number;
}

const VERTEX_SHADER = `
    attribute vec2 aPosition;
    void main() {
        gl_Position = vec4(aPosition, 0.0, 1.0);
    }
`;

const FRAGMENT_SHADER = `
    precision mediump float;
    uniform vec2 iResolution;
    uniform float iTime;
    uniform float uHue;
    uniform float uXOffset;
    uniform float uSpeed;
    uniform float uIntensity;
    uniform float uSize;

    #define OCTAVE_COUNT 10

    vec3 hsv2rgb(vec3 c) {
        vec3 rgb = clamp(abs(mod(c.x * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
        return c.z * mix(vec3(1.0), rgb, c.y);
    }

    float hash11(float p) {
        p = fract(p * .1031);
        p *= p + 33.33;
        p *= p + p;
        return fract(p);
    }

    float hash12(vec2 p) {
        vec3 p3 = fract(vec3(p.xyx) * .1031);
        p3 += dot(p3, p3.yzx + 33.33);
        return fract((p3.x + p3.y) * p3.z);
    }

    mat2 rotate2d(float theta) {
        float c = cos(theta);
        float s = sin(theta);
        return mat2(c, -s, s, c);
    }

    float noise(vec2 p) {
        vec2 ip = floor(p);
        vec2 fp = fract(p);
        float a = hash12(ip);
        float b = hash12(ip + vec2(1.0, 0.0));
        float c = hash12(ip + vec2(0.0, 1.0));
        float d = hash12(ip + vec2(1.0, 1.0));
        vec2 t = smoothstep(0.0, 1.0, fp);
        return mix(mix(a, b, t.x), mix(c, d, t.x), t.y);
    }

    float fbm(vec2 p) {
        float value = 0.0;
        float amplitude = 0.5;
        for (int i = 0; i < OCTAVE_COUNT; ++i) {
            value += amplitude * noise(p);
            p *= rotate2d(0.45);
            p *= 2.0;
            amplitude *= 0.5;
        }
        return value;
    }

    void mainImage(out vec4 fragColor, in vec2 fragCoord) {
        vec2 uv = fragCoord / iResolution.xy;
        uv = 2.0 * uv - 1.0;
        uv.x *= iResolution.x / iResolution.y;
        uv.x += uXOffset;
        uv += 2.0 * fbm(uv * uSize + 0.8 * iTime * uSpeed) - 1.0;

        float dist = abs(uv.x);
        vec3 baseColor = hsv2rgb(vec3(uHue / 360.0, 0.7, 0.8));
        vec3 col = baseColor * pow(mix(0.0, 0.07, hash11(iTime * uSpeed)) / dist, 1.0) * uIntensity;
        col = pow(col, vec3(1.0));
        fragColor = vec4(col, 1.0);
    }

    void main() {
        mainImage(gl_FragColor, gl_FragCoord.xy);
    }
`;

function compileShader(
    gl: WebGLRenderingContext,
    source: string,
    type: number,
): WebGLShader | null {
    const shader = gl.createShader(type);
    if (!shader) {
        return null;
    }
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        gl.deleteShader(shader);
        return null;
    }
    return shader;
}

function linkProgram(
    gl: WebGLRenderingContext,
    vertex: WebGLShader,
    fragment: WebGLShader,
): WebGLProgram | null {
    const program = gl.createProgram();
    if (!program) {
        return null;
    }
    gl.attachShader(program, vertex);
    gl.attachShader(program, fragment);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        gl.deleteProgram(program);
        return null;
    }
    return program;
}

/**
 * WebGL lightning shader background. Six tunable parameters, all
 * optional. Silent-fails when WebGL isn't available.
 */
export function LightningBackground({
    hue = 230,
    xOffset = 0,
    speed = 1,
    intensity = 1,
    size = 1,
    className,
}: LightningTuning & BackgroundProps) {
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const canvas = canvasRef.current;
        if (!canvas) {
            return;
        }

        const resizeCanvas = () => {
            canvas.width = canvas.clientWidth;
            canvas.height = canvas.clientHeight;
        };
        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);

        const gl = canvas.getContext('webgl');
        if (!gl) {
            return () => window.removeEventListener('resize', resizeCanvas);
        }

        const vertexShader = compileShader(gl, VERTEX_SHADER, gl.VERTEX_SHADER);
        const fragmentShader = compileShader(gl, FRAGMENT_SHADER, gl.FRAGMENT_SHADER);
        if (!vertexShader || !fragmentShader) {
            return () => window.removeEventListener('resize', resizeCanvas);
        }

        const program = linkProgram(gl, vertexShader, fragmentShader);
        if (!program) {
            return () => window.removeEventListener('resize', resizeCanvas);
        }
        gl.useProgram(program);

        const vertices = new Float32Array([-1, -1, 1, -1, -1, 1, -1, 1, 1, -1, 1, 1]);
        const vertexBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

        const aPosition = gl.getAttribLocation(program, 'aPosition');
        gl.enableVertexAttribArray(aPosition);
        gl.vertexAttribPointer(aPosition, 2, gl.FLOAT, false, 0, 0);

        const iResolution = gl.getUniformLocation(program, 'iResolution');
        const iTime = gl.getUniformLocation(program, 'iTime');
        const uHue = gl.getUniformLocation(program, 'uHue');
        const uXOffset = gl.getUniformLocation(program, 'uXOffset');
        const uSpeed = gl.getUniformLocation(program, 'uSpeed');
        const uIntensity = gl.getUniformLocation(program, 'uIntensity');
        const uSize = gl.getUniformLocation(program, 'uSize');

        const startTime = performance.now();
        let frameId = 0;

        const render = () => {
            gl.viewport(0, 0, canvas.width, canvas.height);
            gl.uniform2f(iResolution, canvas.width, canvas.height);
            gl.uniform1f(iTime, (performance.now() - startTime) / 1000.0);
            gl.uniform1f(uHue, hue);
            gl.uniform1f(uXOffset, xOffset);
            gl.uniform1f(uSpeed, speed);
            gl.uniform1f(uIntensity, intensity);
            gl.uniform1f(uSize, size);
            gl.drawArrays(gl.TRIANGLES, 0, 6);
            frameId = requestAnimationFrame(render);
        };
        frameId = requestAnimationFrame(render);

        return () => {
            cancelAnimationFrame(frameId);
            window.removeEventListener('resize', resizeCanvas);
            gl.deleteBuffer(vertexBuffer);
            gl.deleteProgram(program);
            gl.deleteShader(vertexShader);
            gl.deleteShader(fragmentShader);
        };
    }, [hue, xOffset, speed, intensity, size]);

    return <canvas ref={canvasRef} className={cn('h-full w-full', className)} />;
}

/**
 * Default tuning that ships as the "lightning" slug. Any custom
 * colourway (e.g. a "lightning-purple" variant) should register its
 * own manifest importing this component with different props.
 */
function LightningDefault({ className }: BackgroundProps) {
    return (
        <LightningBackground
            className={className}
            hue={220}
            speed={1.6}
            intensity={0.6}
            size={2}
        />
    );
}

export const lightningManifest: BackgroundManifest = {
    slug: 'lightning',
    name: 'Raios azuis',
    description: 'Shader WebGL de raios elétricos em tom azul.',
    Component: LightningDefault,
};
