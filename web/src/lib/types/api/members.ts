export enum BoardRole {
    Owner = "Owner",
    Admin = "Admin",
    Editor = "Editor",
    Viewer = "Viewer"
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