import { deleteBoard, updateBoard } from '$lib/boards.js';
import type { UpdateBoardRequest } from '$lib/types/api/boards';

export async function DELETE({ params, cookies }) {
  const boardId = params.boardId;
  const result = await deleteBoard(cookies.get('accessToken'), boardId);
  if (result) {
    return new Response(null, { status: 204 });
  } else {
    return new Response(JSON.stringify({ error: 'Board not found' }), { status: 404 });
  }
}

export async function PATCH({ params, request, cookies }) {
  const boardId = params.boardId;
  const data = await request.json() as UpdateBoardRequest;
  const result = await updateBoard(cookies.get('accessToken'), boardId, data);
  if (result) {
    return new Response(null, { status: 204 });
  } else {
    return new Response(JSON.stringify({ error: 'Board not found' }), { status: 404 });
  }
}
