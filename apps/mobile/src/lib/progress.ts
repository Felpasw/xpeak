/**
 * Percent of `target` reached by `value`, clamped to the 0..100
 * range. Non-positive targets return 0 so divide-by-zero never
 * leaks into a style value. Shared between the solo ProgressBar
 * atom and the per-row bar inside challenge member rankings.
 */
export function clampPercent(value: number, target: number): number {
    if (target <= 0) {
        return 0;
    }
    const raw = (value / target) * 100;
    if (raw < 0) {
        return 0;
    }
    if (raw > 100) {
        return 100;
    }
    return raw;
}
