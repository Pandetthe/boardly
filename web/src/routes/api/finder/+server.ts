import { env } from "$env/dynamic/private";

export async function GET({ request, cookies }) {
    const accessToken = cookies.get('accessToken');
    if (!accessToken) {
        return new Response(JSON.stringify({ error: 'Unauthorized' }), { status: 401 });
    }

    try {
        const accessToken = cookies.get('accessToken');
        const response = await fetch(
            `${env.API_SERVER}users?q=${request.url.split('/').pop()}`,
            {
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": "Bearer " + accessToken
                }
            }
        )

        if (!response.ok) {
            throw new Error('Failed to fetch servers');
        }

        return new Response(await response.text(), { status: 200 });
    } catch (error) {
        return new Response(JSON.stringify({ error: error.message }), { status: 500 });
    }
}