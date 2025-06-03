import { parseDetailedBoard, type DetailedBoard, type DetailedBoardReponse } from '$lib/types/api/boards';
import type { PageServerLoad } from './$types';
import { env } from '$env/dynamic/private';
import { redirect } from '@sveltejs/kit';

export const load = (async ({ cookies, params }) => {
    const accessToken = cookies.get('access_token');
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(new URL(`boards/${params.board}`, env.API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        if (res.ok) {
            const rawBoard = await res.json() as DetailedBoardReponse;
            return { board: parseDetailedBoard(rawBoard) satisfies DetailedBoard };
        }
    } catch (error) {
        console.error('Error while fetching boards:', error);
    }
    redirect(302, "/");
}) satisfies PageServerLoad;