// const baseUrl = '';
const baseUrl = import.meta.env.VITE_API_URL;

export const apiUrl = (path: string) => `${baseUrl}${path}`;
