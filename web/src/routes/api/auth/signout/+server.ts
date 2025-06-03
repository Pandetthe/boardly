import { env } from '$env/dynamic/private';

export async function POST({ cookies }) {
    const accessToken = cookies.get('access_token');
    if (!accessToken)
        throw new Error('Unauthorized: No access token found');
    const res = await fetch(`${env.API_SERVER}/auth/revoke`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${accessToken}`,
        },
    });

    if (!res.ok) {
        return res;
    }

    cookies.delete('access_token', {
        httpOnly: true,
        secure: true,
        sameSite: 'strict',
        path: '/',
    });

    cookies.delete('refresh_token', {
        httpOnly: true,
        secure: true,
        sameSite: 'strict',
        path: '/',
    });

    return new Response();
}
