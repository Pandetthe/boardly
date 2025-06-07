import type { CreateListRequest, DetailedListResponse } from "./lists";
import type { Tag } from "./tags";

export interface CreateSwimlaneRequest extends UpdateSwimlaneRequest {
    lists: CreateListRequest[];
}

export type UpdateSwimlaneRequest = Omit<Swimlane, 'id' | 'createdAt' | 'updatedAt'>;

export type DetailedSwimlaneRequest = Omit<DetailedSwimlane, 'id' | 'createdAt' | 'updatedAt'>;

export interface Swimlane {
    id: string;
    title: string;
    createdAt: Date;
    updatedAt: Date;
}

export interface SwimlaneResponse {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
}

export function parseSwimlane(raw: SwimlaneResponse): Swimlane {
    return {
        ...raw,
        createdAt: new Date(raw.createdAt),
        updatedAt: new Date(raw.updatedAt),
    };
}    

export interface DetailedSwimlaneResponse extends SwimlaneResponse {
    lists: DetailedListResponse[];
    tags: Tag[];
}

export interface DetailedSwimlane extends Swimlane {
    lists: DetailedListResponse[];
    tags: Tag[];
}

export function parseDetailedSwimlane(raw: DetailedSwimlaneResponse): DetailedSwimlane {
    return {
        ...parseSwimlane(raw),
        lists: raw.lists,
    };
}