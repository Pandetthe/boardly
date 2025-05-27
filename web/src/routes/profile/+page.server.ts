import type { PageServerLoad } from './$types';

export const load = (async ({ cookies }) => {
    const accessToken = cookies.get('accessToken');
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    const res = await fetch('http://localhost:5274/users/me', {
        method: 'GET',
        headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });
    
        if (res.ok) {
            return { user: await res.json() };
        }
    return {};
}) satisfies PageServerLoad;