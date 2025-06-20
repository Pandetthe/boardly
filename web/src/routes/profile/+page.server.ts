import type { PageServerLoad } from './$types';
import { env } from '$env/dynamic/public'

export const load = (async ({ cookies }) => {
    const accessToken = cookies.get('access_token');
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    const res = await fetch(`${env.PUBLIC_API_SERVER}/users/me`, {
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