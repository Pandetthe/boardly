import type { LayoutServerLoad } from './$types';

export const load = (async ({ locals }) => {
    return { isAuthenticated: locals.isAuthenticated};
}) satisfies LayoutServerLoad;