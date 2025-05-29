import { env } from '$env/dynamic/private';
import type { Board, BoardResponse, CreateBoardRequest, DetailedBoardReponse, UpdateBoardRequest } from '$lib/types/api/boards';
import { parseBoard, parseDetailedBoard } from '$lib/types/api/boards';

export const getBoards = (async (accessToken: string | undefined) => {
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(`${env.API_SERVER}/boards/`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        if (res.ok) {
            const rawBoards = await res.json() as BoardResponse[];
            return rawBoards.map(parseBoard) satisfies Board[];
        }
    } catch (error) {
        console.error('Error while fetching boards:', error);
    }
    return null;
});

export const getBoard = (async (accessToken: string | undefined, boardId: string) => {
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(`${env.API_SERVER}/boards/${boardId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });

        if (res.ok) {
            const rawBoard = await res.json() as DetailedBoardReponse
            return parseDetailedBoard(rawBoard);
        }
    } catch (error) {
        console.error('Error while fetching a board:', error);
    }
    return null;
});

export const createBoard = (async (accessToken: string | undefined, board: CreateBoardRequest) => {
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(`${env.API_SERVER}/boards`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
            body: JSON.stringify(board),
        });
        return res.ok;
    } catch (error) {
        console.error('Error while creating a board', error);
    }
});

export const updateBoard = (async (accessToken: string | undefined, boardId: string, board: UpdateBoardRequest) => {
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(`${env.API_SERVER}/boards/${boardId}`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
            body: JSON.stringify(board),
        });
        console.log(await res.json());
        return res.ok;
    } catch (error) {
        console.error('Error while updating a board', error);
    }
});

export const deleteBoard = (async (accessToken: string | undefined, boardId: string) => {
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(`${env.API_SERVER}/boards/${boardId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
        });
        return res.ok;
    } catch (error) {
        console.error('Error while deleting a board:', error);
    }
});