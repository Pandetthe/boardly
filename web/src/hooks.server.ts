import type { Handle } from '@sveltejs/kit';

const PUBLIC_ROUTES = ['test'];
const UNAUTH_ONLY_ROUTES = ['/signin', '/signup'];

export const handle: Handle = async ({ event, resolve }) => {
	const { pathname, origin } = event.url;
	if (pathname.startsWith('/auth/')) {
		return resolve(event);
	}
	let accessToken = event.cookies.get('accessToken');
	let refreshToken = event.cookies.get('refreshToken');
	if (!accessToken && refreshToken) {
		const res = await fetch(`http://localhost:5274/auth/refresh`, {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ refreshToken: refreshToken })
		});
        if (res.ok) {
            const data = await res.json(); // todo set type
            accessToken = data.accessToken;
            refreshToken = data.refreshToken;
            event.cookies.set('accessToken', accessToken!, { // todo
                httpOnly: true, 
                secure: true, 
                sameSite: 'strict', 
                maxAge: data.accessTokenExpiresIn,
                path: '/',
            });
        
            event.cookies.set('refreshToken', refreshToken!, { // todo
                httpOnly: true, 
                secure: true, 
                sameSite: 'strict', 
                maxAge: data.refreshTokenExpiresIn,
                path: '/',
            });
        }

	}
	const isAuthenticated = !!accessToken;
    if (isAuthenticated && UNAUTH_ONLY_ROUTES.includes(pathname)) {
		return Response.redirect(`${origin}/`, 303);
	}

	if (!isAuthenticated && !PUBLIC_ROUTES.includes(pathname) && !UNAUTH_ONLY_ROUTES.includes(pathname)) {
		return Response.redirect(`${origin}/signin`, 303);
	}
	return resolve(event);
};
