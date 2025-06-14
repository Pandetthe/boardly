import type { CreateListRequest, List, ListResponse } from "./lists";
import type { Tag } from "./tags";

export interface CreateSwimlaneRequest extends UpdateSwimlaneRequest {
    lists: CreateListRequest[];
}

export type UpdateSwimlaneRequest = Omit<Swimlane, 'id' | 'createdAt' | 'updatedAt'>;

export type DetailedSwimlaneRequest = Omit<DetailedSwimlane, 'id' | 'createdAt' | 'updatedAt'>;

export type Swimlane = SwimlaneResponse;

export interface SwimlaneResponse {
    id: string;
    title: string;
}

export interface DetailedSwimlaneResponse extends SwimlaneResponse {
    lists: ListResponse[];
    tags: Tag[];
}

export interface DetailedSwimlane extends Swimlane {
    lists: List[];
    tags: Tag[];
}