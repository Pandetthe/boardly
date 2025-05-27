export async function POST({ request, cookies }) { 
    const { nickname, password } = await request.json();
    const res = await fetch('http://localhost:5274/auth/signup', {
      method: 'POST',
      headers: {'Content-Type': 'application/json',},
      body: JSON.stringify({ nickname, password }),
    });
  
    if (!res.ok) {
      return res;
    }
  
    const { accessToken, accessTokenExpiresIn, refreshToken, refreshTokenExpiresIn } = await res.json();
    cookies.set('accessToken', accessToken, {
      httpOnly: true, 
      secure: true, 
      sameSite: 'strict', 
      maxAge: accessTokenExpiresIn, 
      path: '/',
    });
  
    cookies.set('refreshToken', refreshToken, {
      httpOnly: true, 
      secure: true, 
      sameSite: 'strict', 
      maxAge: refreshTokenExpiresIn, 
      path: '/',
    });
  
    return new Response('Signup in successfully');
  }
  