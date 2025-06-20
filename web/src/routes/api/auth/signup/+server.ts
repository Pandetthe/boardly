import type { AuthResponse, SignUpRequest } from '$lib/types/api/auth';
import { env } from '$env/dynamic/public';

export async function POST({ request, cookies }) {
  const { nickname, password } = await request.json();
  const res = await fetch(new URL('/auth/signup', env.PUBLIC_API_SERVER), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', },
    body: JSON.stringify({ nickname, password } as SignUpRequest),
  });

  if (!res.ok) {
    return res;
  }

  const { accessToken, accessTokenExpiresIn, refreshToken, refreshTokenExpiresIn } = await res.json() as AuthResponse;

  cookies.set('access_token', accessToken, {
    httpOnly: true,
    secure: true,
    sameSite: 'strict',
    maxAge: accessTokenExpiresIn,
    path: '/',
  });

  cookies.set('refresh_token', refreshToken, {
    httpOnly: true,
    secure: true,
    sameSite: 'strict',
    maxAge: refreshTokenExpiresIn,
    path: '/',
  });

  return new Response();
}
