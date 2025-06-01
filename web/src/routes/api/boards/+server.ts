import { createBoard } from '$lib/boards.js';
import type { CreateBoardRequest } from '$lib/types/api/boards';

export async function POST({ request, cookies }) {
    const data = await request.json() as CreateBoardRequest;
    const result = await createBoard(cookies.get('accessToken'), data);
    if (result) {
        return new Response(null, { status: 204 });
    } else {
        return new Response(JSON.stringify({ error: 'Board not found' }), { status: 404 });
    }
}
