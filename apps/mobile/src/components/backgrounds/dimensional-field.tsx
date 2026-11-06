'use client';

import { Backgrounds } from '@xpeak/shared';
import { useEffect, useRef } from 'react';

import { cn } from '@/lib/utils';

import type { BackgroundManifest, BackgroundProps } from './types';

const BACKGROUND = '#030712';

const VERT = `
attribute vec2 a_position;
void main() {
  gl_Position = vec4(a_position, 0.0, 1.0);
}
`;

const FRAG = `
#ifdef GL_FRAGMENT_PRECISION_HIGH
precision highp float;
#else
precision mediump float;
#endif

uniform vec3 u_resolution;
uniform float u_time;
uniform vec3 u_color1;
uniform vec3 u_color2;

vec3 permute(vec3 x) { return mod(((x * 34.0) + 1.0) * x, 289.0); }

float snoise(vec2 v) {
  const vec4 C = vec4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
  vec2 i  = floor(v + dot(v, C.yy));
  vec2 x0 = v - i + dot(i, C.xx);
  vec2 i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
  vec4 x12 = x0.xyxy + C.xxzz;
  x12.xy -= i1;
  i = mod(i, 289.0);
  vec3 p = permute(permute(i.y + vec3(0.0, i1.y, 1.0)) + i.x + vec3(0.0, i1.x, 1.0));
  vec3 m = max(0.5 - vec3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
  m = m * m; m = m * m;
  vec3 x = 2.0 * fract(p * C.www) - 1.0;
  vec3 h = abs(x) - 0.5;
  vec3 ox = floor(x + 0.5);
  vec3 a0 = x - ox;
  m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
  vec3 g;
  g.x  = a0.x  * x0.x  + h.x  * x0.y;
  g.yz = a0.yz * x12.xz + h.yz * x12.yw;
  return 130.0 * dot(m, g);
}

vec3 flowField(vec2 uv, float t) {
  vec2 st = uv * 0.5;
  st += vec2(snoise(st + t * 0.04), snoise(st - t * 0.04)) * 0.4;
  float beam = smoothstep(0.08, 0.8, snoise(vec2(st.x + st.y * 2.0 - t * 0.1, t * 0.03)));
  vec3 glow = mix(u_color1, u_color2, snoise(uv * 2.0 + t * 0.15) * 0.5 + 0.5);
  vec3 base = vec3(0.03, 0.04, 0.08);
  vec3 lit = mix(base, glow * 1.25, clamp(beam * 1.15, 0.0, 1.0));
  return lit + glow * 0.12;
}

// One impostor sphere: analytic 2D disk with fake normal, fresnel rim and
// an internal flow lift that mirrors the background.
vec4 impostorSphere(vec2 frag, vec2 center, float radius, float t, vec2 res) {
  vec2 pd = frag - center;
  float d = length(pd);
  if (d > radius) return vec4(0.0);
  float z = sqrt(max(radius * radius - d * d, 0.0));
  vec3 normal = normalize(vec3(pd.x, pd.y, z));
  float fresnel = pow(clamp(1.0 - normal.z, 0.0, 1.0), 2.0);

  vec2 uv = frag / res.xy;
  vec3 flow = flowField(uv + normal.xy * 0.2, t);
  vec3 base = vec3(0.08, 0.09, 0.17);
  vec3 body = mix(base, flow * 1.35, clamp(fresnel * 1.15 + 0.5, 0.0, 1.15));
  body += u_color1 * fresnel * 0.55;
  body += u_color2 * fresnel * fresnel * 0.3;

  float rim = smoothstep(radius, radius - radius * 0.015, d);
  return vec4(body, rim);
}

void main() {
  vec2 res = u_resolution.xy;
  float dpr = u_resolution.z;
  vec2 frag = gl_FragCoord.xy;
  vec2 uv = frag / res;

  vec3 col = flowField(vec2(uv.x * (res.x / res.y), uv.y), u_time);

  // Four spheres bob and float, positioned as a fraction of the min viewport
  // dimension so they read the same on any aspect.
  float unit = min(res.x, res.y);
  float t = u_time;
  vec2 c1 = vec2(res.x * 0.74, res.y * 0.54 + sin(t * 0.28) * unit * 0.025);
  vec2 c2 = vec2(res.x * 0.22, res.y * 0.74 + sin(t * 0.38 + 1.2) * unit * 0.03);
  vec2 c3 = vec2(res.x * 0.30, res.y * 0.26 + sin(t * 0.47 + 2.0) * unit * 0.028);
  vec2 c4 = vec2(res.x * 0.78, res.y * 0.20 + sin(t * 0.61 + 3.5) * unit * 0.04);

  vec4 s1 = impostorSphere(frag, c1, unit * 0.38, t, res);
  vec4 s2 = impostorSphere(frag, c2, unit * 0.16, t, res);
  vec4 s3 = impostorSphere(frag, c3, unit * 0.10, t, res);
  vec4 s4 = impostorSphere(frag, c4, unit * 0.065, t, res);

  col = mix(col, s1.rgb, s1.a);
  col = mix(col, s2.rgb, s2.a);
  col = mix(col, s3.rgb, s3.a);
  col = mix(col, s4.rgb, s4.a);

  float vd = distance(uv, vec2(0.5));
  // Gentle vignette so the aurora still breathes at the edges.
  float vignette = smoothstep(1.3, 0.4, vd);
  vec3 edge = vec3(0.03, 0.038, 0.08);
  col = mix(edge, col, vignette);

  // Gentle saturation lift so bright beams and sphere rims still read as
  // sky/indigo without punching into neon.
  float luma = dot(col, vec3(0.2126, 0.7152, 0.0722));
  col = mix(vec3(luma), col, 1.15);

  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
`;

const SKY_400: [number, number, number] = [56 / 255, 189 / 255, 248 / 255];
const INDIGO_400: [number, number, number] = [129 / 255, 140 / 255, 248 / 255];

function compile(gl: WebGLRenderingContext) {
    const program = gl.createProgram();
    if (!program) {
        return null;
    }
    for (const [type, source] of [
        [gl.VERTEX_SHADER, VERT],
        [gl.FRAGMENT_SHADER, FRAG],
    ] as const) {
        const shader = gl.createShader(type);
        if (!shader) {
            return null;
        }
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
            gl.deleteShader(shader);
            gl.deleteProgram(program);
            return null;
        }
        gl.attachShader(program, shader);
        gl.deleteShader(shader);
    }
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        gl.deleteProgram(program);
        return null;
    }
    return program;
}

function DimensionalFieldRender({ className }: BackgroundProps) {
    const rootRef = useRef<HTMLDivElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const root = rootRef.current;
        const canvas = canvasRef.current;
        if (!root || !canvas) {
            return;
        }
        const gl = canvas.getContext('webgl', { antialias: false, alpha: false });
        if (!gl) {
            return;
        }
        const program = compile(gl);
        if (!program) {
            return;
        }
        const renderRoot = root;
        const renderCanvas = canvas;
        const renderGl = gl;

        const triangleBuffer = renderGl.createBuffer();
        renderGl.bindBuffer(renderGl.ARRAY_BUFFER, triangleBuffer);
        renderGl.bufferData(
            renderGl.ARRAY_BUFFER,
            new Float32Array([-1, -1, 3, -1, -1, 3]),
            renderGl.STATIC_DRAW,
        );
        const position = renderGl.getAttribLocation(program, 'a_position');
        renderGl.enableVertexAttribArray(position);
        renderGl.vertexAttribPointer(position, 2, renderGl.FLOAT, false, 0, 0);
        renderGl.useProgram(program);

        const u = (name: string) => renderGl.getUniformLocation(program, name);
        const loc = {
            resolution: u('u_resolution'),
            time: u('u_time'),
            color1: u('u_color1'),
            color2: u('u_color2'),
        };
        renderGl.uniform3fv(loc.color1, SKY_400);
        renderGl.uniform3fv(loc.color2, INDIGO_400);

        let reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        let time = 0;
        let last = 0;
        let raf = 0;
        let visible = true;

        function resize() {
            const dpr = Math.min(window.devicePixelRatio || 1, 2);
            const w = Math.max(1, Math.round(renderCanvas.clientWidth * dpr));
            const h = Math.max(1, Math.round(renderCanvas.clientHeight * dpr));
            if (renderCanvas.width !== w || renderCanvas.height !== h) {
                renderCanvas.width = w;
                renderCanvas.height = h;
            }
        }

        function draw(now: number) {
            raf = 0;
            const dt = last ? Math.min((now - last) / 1000, 0.05) : 0.016;
            last = now;
            resize();
            if (!reduceMotion) {
                time += dt;
            }
            const dpr = renderCanvas.width / Math.max(1, renderCanvas.clientWidth);
            renderGl.viewport(0, 0, renderCanvas.width, renderCanvas.height);
            renderGl.uniform3f(loc.resolution, renderCanvas.width, renderCanvas.height, dpr);
            renderGl.uniform1f(loc.time, time);
            renderGl.drawArrays(renderGl.TRIANGLES, 0, 3);
            if (!reduceMotion && visible && !document.hidden) {
                raf = requestAnimationFrame(draw);
            }
        }

        function kick() {
            if (!raf && visible && !document.hidden) {
                raf = requestAnimationFrame(draw);
            }
        }

        function stop() {
            if (raf) {
                cancelAnimationFrame(raf);
                raf = 0;
            }
        }

        resize();
        kick();

        const motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
        const handleMotionPreference = () => {
            reduceMotion = motionPreference.matches;
            last = 0;
            if (reduceMotion) {
                requestAnimationFrame(draw);
            } else {
                kick();
            }
        };
        motionPreference.addEventListener('change', handleMotionPreference);

        const resizeObserver = new ResizeObserver(() => {
            resize();
            last = 0;
            kick();
        });
        resizeObserver.observe(renderCanvas);

        const visibilityObserver = new IntersectionObserver(([entry]) => {
            visible = Boolean(entry?.isIntersecting);
            if (visible) {
                last = 0;
                kick();
            } else {
                stop();
            }
        });
        visibilityObserver.observe(renderRoot);

        const onVisibility = () => {
            if (document.hidden) {
                stop();
            } else {
                last = 0;
                kick();
            }
        };
        document.addEventListener('visibilitychange', onVisibility);

        return () => {
            stop();
            resizeObserver.disconnect();
            visibilityObserver.disconnect();
            motionPreference.removeEventListener('change', handleMotionPreference);
            document.removeEventListener('visibilitychange', onVisibility);
            renderGl.deleteBuffer(triangleBuffer);
            renderGl.deleteProgram(program);
        };
    }, []);

    return (
        <div
            ref={rootRef}
            aria-hidden="true"
            className={cn('relative overflow-hidden', className)}
            style={{ backgroundColor: BACKGROUND }}
        >
            <canvas
                ref={canvasRef}
                aria-hidden="true"
                className="pointer-events-none absolute inset-0 block h-full w-full"
            />
        </div>
    );
}

function DimensionalFieldDefault({ className }: BackgroundProps) {
    return <DimensionalFieldRender className={className} />;
}

export const dimensionalFieldManifest: BackgroundManifest = {
    slug: Backgrounds.DimensionalField,
    name: 'Dimensional',
    description: 'Campo noise com esferas flutuantes em tom sky + indigo.',
    Component: DimensionalFieldDefault,
};
