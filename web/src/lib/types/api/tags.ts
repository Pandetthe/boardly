export type Tag = TagResponse;

export interface TagResponse {
    id: string;
    title: string;
    color: string;
}

export interface CreateTagRequest {
    title: string;
    color: string;
}