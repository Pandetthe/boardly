import type { SwimlaneResponse } from "./swimlanes";

export type CreateBoardRequest = Omit<Board, 'id' | 'createdAt' | 'updatedAt'>;

export type UpdateBoardRequest = Partial<Omit<Board, 'id' | 'createdAt' | 'updatedAt'>>;

export interface Board {
    id: string;
    title: string;
    createdAt: Date;
    updatedAt: Date;
}

export interface BoardResponse {
    id: string;
    title: string;
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

export interface Member extends MemberResponse {
    id: string;
    nickname: string;
    role: BoardRole;
}


export enum BoardRole {
    Owner = "Owner",
    Admin = "Admin",
    Editor = "Editor",
    Viewer = "Viewer"
}


export interface DetailedBoardReponse extends BoardResponse {
    swimlanes: SwimlaneResponse[];
    members: MemberResponse[];
}

export interface MemberResponse {
    id: string;
    role: BoardRole;
}

export interface MemberRequest extends MemberResponse {}