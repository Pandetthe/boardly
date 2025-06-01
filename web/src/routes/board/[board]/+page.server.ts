import type { PageServerLoad } from './$types';
import { redirect } from '@sveltejs/kit';
import { getBoard } from '$lib/boards';

export const load = (async ({ cookies, params }) => {
    const accessToken = cookies.get('accessToken');
    const board = await getBoard(accessToken, params.board);
    if (!board) {
        return redirect(302, '/');
    }
    return { board: board };
}) satisfies PageServerLoad;