export interface Tag {
    id: string;
    title: string;
    color: string;
}

export interface CreateTagRequest {
    title: string;
    color: string;
}