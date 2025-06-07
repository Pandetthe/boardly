export enum BoardRole {
    Owner = "owner",
    Admin = "admin",
    Editor = "editor",
    Viewer = "viewer"
}

export interface Member extends MemberResponse {
}

export interface MemberResponse {
    userId: string;
    nickname: string;
    role: BoardRole;
    isActive: boolean;
}

export interface MemberRequest extends MemberResponse {}