import { createSwimlane } from '$lib/swimlanes';
import type { CreateSwimlaneRequest } from '$lib/types/api/swimlanes';

export async function POST({ request, cookies, params }) {
    const boardId = params.boardId;
    const data = await request.json() as CreateSwimlaneRequest;
    const result = await createSwimlane(cookies.get('accessToken'), boardId, data);
    if (result) {
        return new Response(null, { status: 204 });
    } else {
        return new Response(JSON.stringify({ error: 'Swimlane not found' }), { status: 404 });
    }
}
