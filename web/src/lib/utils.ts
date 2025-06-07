export function idToColor(id: string) {
    const colors = [
        "#0070F3",
        "#FFB224",
        "#FFB224",
        "#46A758",
        "#12A594",
        "#8E4EC6",
        "#E93D82",
    ];
    const index = hashString(id) % colors.length;
    return colors[index];
}

export function hashString(str: string) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
        hash = (hash << 5) - hash + str.charCodeAt(i);
        hash |= 0;
    }
    return Math.abs(hash);
}