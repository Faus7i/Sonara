export async function GET(request: Request) {
  return proxy(request, 'GET');
}

export async function POST(request: Request) {
  return proxy(request, 'POST');
}

export async function PUT(request: Request) {
  return proxy(request, 'PUT');
}

export async function DELETE(request: Request) {
  return proxy(request, 'DELETE');
}

async function proxy(request: Request, _method: string) {
  const url = new URL(request.url);
  const targetUrl = `http://127.0.0.1:5000${url.pathname}${url.search}`;

  try {
    const headers: Record<string, string> = {};
    const auth = request.headers.get('authorization');
    if (auth) headers['Authorization'] = auth;
    const ct = request.headers.get('content-type');
    if (ct) headers['Content-Type'] = ct;

    const res = await fetch(targetUrl, {
      method: request.method,
      headers,
      body: request.method !== 'GET' && request.method !== 'HEAD'
        ? await request.text()
        : undefined,
    });

    const data = await res.json();
    return Response.json(data, { status: res.status });
  } catch {
    return Response.json(
      { success: false, message: '后端服务不可用', errors: [] },
      { status: 502 }
    );
  }
}

export const dynamic = 'force-dynamic';
