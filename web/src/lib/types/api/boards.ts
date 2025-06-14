import type { Member, MemberResponse } from "./members";
import type { CreateSwimlaneRequest, DetailedSwimlane, DetailedSwimlaneResponse } from "./swimlanes";

export interface CreateBoardRequest extends UpdateBoardRequest {
    swimlanes: CreateSwimlaneRequest[];
}

export type UpdateBoardRequest = Omit<Board, 'id' | 'createdAt' | 'updatedAt'>;

export interface Board {
    id: string;
    title: string;
    members: Member[];
    createdAt: Date;
    updatedAt: Date;
}

export interface DetailedBoard extends Board {
    swimlanes: DetailedSwimlane[];
}

export interface BoardResponse {
    id: string;
    title: string;
    members: MemberResponse[];
    createdAt: string;
    updatedAt: string;
}

export function parseBoard(raw: BoardResponse): Board {
	return {
		...raw,
		createdAt: new Date(raw.createdAt),
		updatedAt: new Date(raw.updatedAt),
	};
}


export interface DetailedBoardReponse extends BoardResponse {
    swimlanes: DetailedSwimlaneResponse[];
}

export function parseDetailedBoard(raw: DetailedBoardReponse): DetailedBoard {
    return {
        ...parseBoard(raw),
        swimlanes: raw.swimlanes 
    };
}