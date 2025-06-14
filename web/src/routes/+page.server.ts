import { parseBoard, type Board, type BoardResponse } from '$lib/types/api/boards';
import type { PageServerLoad } from './$types';
import { env } from '$env/dynamic/private';
import { parseUser, type UserResponse } from '$lib/types/api/users';

export const load = (async ({ cookies, depends }) => {
    depends('api:boards');
    const accessToken = cookies.get('access_token');
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(new URL("boards", env.VITE_API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        const user = await fetch(new URL("users/me", env.VITE_API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });
        if (res.ok && user.ok) {
            const rawBoards = await res.json() as BoardResponse[];
            const rawUser = await user.json() as UserResponse;
            return { boards: rawBoards.map(parseBoard) satisfies Board[], user: parseUser(rawUser)};
        }
    } catch (error) {
        console.error('Error while fetching boards:', error);
    }
    return { boards: [] satisfies Board[] };
}) satisfies PageServerLoad;