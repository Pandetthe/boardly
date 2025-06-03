import { parseDetailedBoard, type DetailedBoard, type DetailedBoardReponse } from '$lib/types/api/boards';
import type { PageServerLoad } from './$types';
import { env } from '$env/dynamic/private';
import { redirect } from '@sveltejs/kit';

export const load = (async ({ cookies, params, depends }) => {
    depends('api:boards');
    const accessToken = cookies.get('access_token');
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(new URL(`boards/${params.board}`, env.VITE_API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        const cards = await fetch(new URL(`boards/${params.board}/cards`, env.VITE_API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        if (res.ok && cards.ok) {
            const rawBoard = await res.json() as DetailedBoardReponse;
            return { board: parseDetailedBoard(rawBoard) satisfies DetailedBoard, cards: await cards.json() };
        }
    } catch (error) {
        console.error('Error while fetching boards:', error);
    }
    redirect(302, "/");
}) satisfies PageServerLoad;