// const baseUrl = '';
const baseUrl = import.meta.env.VITE_API_URL;

export const apiUrl = (path: string) => {
    if (!baseUrl) {
        return path;
    }

    // 清除baseUrl末尾的所有斜杠
    const normalizedBase = baseUrl.replace(/\/+$/, '');
    // 清除path开头的所有斜杠
    const normalizedPath = path.replace(/^\/+/, '');

    // 只有当两者都非空时才添加连接斜杠，避免空字符串情况
    return normalizedBase && normalizedPath
        ? `${normalizedBase}/${normalizedPath}`
        : normalizedBase + normalizedPath;
};
