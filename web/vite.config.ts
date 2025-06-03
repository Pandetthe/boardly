import tailwindcss from '@tailwindcss/vite';	
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode}) => {
	const env = loadEnv(mode, process.cwd(), '');
	return {
		plugins: [tailwindcss(), sveltekit()],
		server: {
			proxy: {
				'/api/hubs': {
					target: env.VITE_API_SERVER,
					changeOrigin: true,
					ws: true,     
					secure: false,
					rewrite: path => path.replace(/^\/api\/hubs/, 'hubs')
				}
			}
		}
	};
});

