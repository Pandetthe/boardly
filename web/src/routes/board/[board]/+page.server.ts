import { parseDetailedBoard, type DetailedBoard, type DetailedBoardReponse } from '$lib/types/api/boards';
import type { PageServerLoad } from './$types';
import { env } from '$env/dynamic/public';
import { redirect } from '@sveltejs/kit';
import { parseCard, type Card, type CardResponse } from '$lib/types/api/cards';
import { parseUser, type User, type UserResponse } from '$lib/types/api/users';

export const load = (async ({ cookies, params, depends }) => {
    depends('api:board');
    const accessToken = cookies.get('access_token');
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(new URL(`boards/${params.board}`, env.PUBLIC_API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        const cards = await fetch(new URL(`boards/${params.board}/cards`, env.PUBLIC_API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        const user = await fetch(new URL(`users/me`, env.PUBLIC_API_SERVER), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });
        if (res.ok && cards.ok && user.ok) {
            const rawBoard = await res.json() as DetailedBoardReponse;
            const rawCards = await cards.json() as CardResponse[];
            const rawUser = await user.json() as UserResponse;
            return {
                board: parseDetailedBoard(rawBoard) satisfies DetailedBoard,
                cards: rawCards.map(parseCard) satisfies Card[],
                user: parseUser(rawUser) satisfies User,
                token: accessToken
            };
        }
    } catch (error) {
        console.error('Error while fetching data:', error);
    }
    redirect(302, "/");
}) satisfies PageServerLoad;