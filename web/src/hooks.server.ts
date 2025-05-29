import type { Handle } from '@sveltejs/kit';
import { env } from '$env/dynamic/private'
import type { RefreshRequest, AuthResponse } from '$lib/types/api/auth';

const PUBLIC_ROUTES: string[] = [];
const UNAUTH_ONLY_ROUTES: string[] = ['/signin', '/signup'];

export const handle: Handle = async ({ event, resolve }) => {
	const { pathname, origin } = event.url;
	if (pathname.startsWith('/api/auth/')) {
		return resolve(event);
	}
	let accessToken = event.cookies.get('accessToken');
	let refreshToken = event.cookies.get('refreshToken');
	if (!accessToken && refreshToken) {
        try {
            const res = await fetch(`${env.API_SERVER}/auth/refresh`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: refreshToken } as RefreshRequest)
            });
            if (res.ok) {
                const data = await res.json() as AuthResponse;
                accessToken = data.accessToken;
                refreshToken = data.refreshToken;
                event.cookies.set('accessToken', accessToken, {
                    httpOnly: true,
                    secure: true,
                    sameSite: 'strict',
                    maxAge: data.accessTokenExpiresIn,
                    path: '/',
                });
            
                event.cookies.set('refreshToken', refreshToken, {
                    httpOnly: true, 
                    secure: true, 
                    sameSite: 'strict', 
                    maxAge: data.refreshTokenExpiresIn,
                    path: '/',
                });
            }
            if (res.status === 401) {
                event.cookies.delete('accessToken', {
                    httpOnly: true,
                    secure: true,
                    sameSite: 'strict',
                    path: '/',
                });
                event.cookies.delete('refreshToken', {
                    httpOnly: true,
                    secure: true,
                    sameSite: 'strict',
                    path: '/',
                });
                accessToken = undefined;
                refreshToken = undefined;
            }
        } catch (error) {
            console.error('Error refreshing token:', error);
        }

	}
	event.locals.isAuthenticated = !!accessToken;
    if (event.locals.isAuthenticated && UNAUTH_ONLY_ROUTES.includes(pathname)) {
		return Response.redirect(`${origin}/`, 303);
	}

	if (!event.locals.isAuthenticated && !PUBLIC_ROUTES.includes(pathname) && !UNAUTH_ONLY_ROUTES.includes(pathname)) {
		return Response.redirect(`${origin}/signin`, 303);
	}
	return resolve(event);
};
