import { env } from "$env/dynamic/private";
import type { CreateSwimlaneRequest, UpdateSwimlaneRequest } from "./types/api/swimlanes";

export const createSwimlane = (async (accessToken: string | undefined, boardId: string, swimlane: CreateSwimlaneRequest) => {
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(`${env.API_SERVER}/boards`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
            body: JSON.stringify(swimlane),
        });
        return res.ok;
    } catch (error) {
        console.error('Error while creating a swimlane', error);
    }
});

export const updateBoard = (async (accessToken: string | undefined, boardId: string, swimlaneId: string, swimlane: UpdateSwimlaneRequest) => {
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    try {
        const res = await fetch(`${env.API_SERVER}/boards/${boardId}/swimlanes/${swimlaneId}`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`,
            },
            body: JSON.stringify(swimlane),
        });
        console.log(await res.json());
        return res.ok;
    } catch (error) {
        console.error('Error while updating a swimlane', error);
    }
});