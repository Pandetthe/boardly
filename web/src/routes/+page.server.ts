import type { PageServerLoad } from './$types';
import { getBoards } from '$lib/boards';

export const load = (async ({ cookies, depends }) => {
    depends('api:boards');
    const data = { boards: await getBoards(cookies.get('accessToken')) };
    return data;
}) satisfies PageServerLoad;