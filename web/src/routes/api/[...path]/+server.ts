import type { RequestHandler } from '@sveltejs/kit';
import { env } from '$env/dynamic/public';

export const GET: RequestHandler = async (event) => handleProxy(event);
export const POST: RequestHandler = async (event) => handleProxy(event);
export const PUT: RequestHandler = async (event) => handleProxy(event);
export const DELETE: RequestHandler = async (event) => handleProxy(event);
export const PATCH: RequestHandler = async (event) => handleProxy(event);

async function handleProxy({ params, request, url, cookies }: Parameters<RequestHandler>[0]) {
	const path = Array.isArray(params.path) ? params.path.join('/') : params.path;
	const fullUrl = new URL(`${path}${url.search ? '?' + url.searchParams.toString() : ''}`, env.PUBLIC_API_SERVER);
	
	const accessToken = cookies.get('access_token');

	const headers = new Headers(request.headers);
	if (accessToken) {
		headers.set('Authorization', `Bearer ${accessToken}`);
	}

	const fetchOptions: RequestInit = {
		method: request.method,
		headers,
		body: ['GET', 'HEAD'].includes(request.method) ? undefined : await request.clone().arrayBuffer(),
		credentials: 'include'
	};

	const response = await fetch(fullUrl, fetchOptions);
	const body = await response.arrayBuffer();
    const responseHeaders = new Headers();
    for (const [key, value] of response.headers.entries()) {
        if (key.toLowerCase() !== 'content-encoding') {
            responseHeaders.set(key, value);
        }
    }
	return new Response(body, {
		status: response.status,
		headers: responseHeaders
	});
}
