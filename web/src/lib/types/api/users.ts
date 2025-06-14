export interface UserResponse {
    id: string;
    nickname: string;
    createdAt: string;
    updatedAt: string;
}

export interface User {
    id: string;
    nickname: string;
    createdAt: Date;
    updatedAt: Date;
}

export function parseUser(raw: UserResponse): User {
    return {
        ...raw,
        createdAt: new Date(raw.createdAt),
        updatedAt: new Date(raw.updatedAt),
    };
}

export type SimplifiedUser = SimplifiedUserResponse;

export interface SimplifiedUserResponse {
    id: string;
    nickname: string;
}