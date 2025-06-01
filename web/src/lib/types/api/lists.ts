export type CreateListRequest = Omit<List, 'id' | 'createdAt' | 'updatedAt'>;

export type UpdateListRequest = Omit<List, 'id' | 'createdAt' | 'updatedAt'>;

export interface List {
    id: string;
    title: string;
    color: string;
    createdAt: Date;
    updatedAt: Date;
}

export interface ListResponse {
    id: string;
    title: string;
    color: string;
    maxWIP: number;
    createdAt: string;
    updatedAt: string;
}

export interface DetailedListResponse extends ListResponse {
}