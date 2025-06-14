export type CreateListRequest = Omit<List, 'id' | 'createdAt' | 'updatedAt'>;

export type UpdateListRequest = Omit<List, 'id' | 'createdAt' | 'updatedAt'>;

export type List = ListResponse;

export interface ListResponse {
    id: string;
    title: string;
    color: string;
    maxWIP: number | null;
}