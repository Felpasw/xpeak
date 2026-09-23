export function generateStars(count: number, starColor: string) {
    const shadows: string[] = [];
    for (let i = 0; i < count; i++) {
        const x = Math.floor(Math.random() * 4000) - 2000;
        const y = Math.floor(Math.random() * 4000) - 2000;
        shadows.push(`${x}px ${y}px ${starColor}`);
    }
    return shadows.join(', ');
}
