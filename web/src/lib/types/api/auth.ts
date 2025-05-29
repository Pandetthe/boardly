export interface RefreshRequest {
    refreshToken: string;
}

export interface AuthResponse {
    accessToken: string;
    accessTokenExpiresIn: number;
    refreshToken: string;
    refreshTokenExpiresIn: number;
}

export interface RevokeRequest {
    refreshToken: string;
}

export interface SignInRequest {
    nickname: string;
    password: string;
}

export interface SignUpRequest {
    nickname: string;
    password: string;
}