import { env } from '$env/dynamic/private';
import type { RequestHandler } from '@sveltejs/kit';

export const GET: RequestHandler = async ({ params, request, url, cookies }) => {
	const path = Array.isArray(params.path) ? params.path.join('/') : params.path;
	const targetUrl = `${env.API_SERVER}/hubs/${path}`;

	const accessToken = cookies.get('access_token');
	if (!accessToken) {
		return new Response('Unauthorized', { status: 401 });
	}

	const proxyUrl = new URL(targetUrl);
	proxyUrl.searchParams.set('access_token', accessToken);

	return Response.redirect(proxyUrl.toString(), 307);
};
