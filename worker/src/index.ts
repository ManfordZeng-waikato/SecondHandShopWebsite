export interface Env {
  IMAGES: R2Bucket;
}

const CACHE_CONTROL = "public, max-age=86400, s-maxage=604800, immutable";

const CORS_HEADERS: Record<string, string> = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "GET, HEAD, OPTIONS",
  "Access-Control-Max-Age": "86400",
};

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: CORS_HEADERS });
    }

    if (request.method !== "GET" && request.method !== "HEAD") {
      return new Response("Method Not Allowed", { status: 405 });
    }

    const url = new URL(request.url);
    const key = decodeURIComponent(url.pathname.slice(1));

    if (!key) {
      return new Response("Bad Request: missing object key", { status: 400 });
    }

    const object = await env.IMAGES.get(key);

    if (!object) {
      return new Response("Not Found", { status: 404 });
    }

    const headers = new Headers(CORS_HEADERS);
    headers.set(
      "Content-Type",
      object.httpMetadata?.contentType ?? "application/octet-stream",
    );
    headers.set("Cache-Control", CACHE_CONTROL);
    headers.set("ETag", object.httpEtag);

    if (object.httpMetadata?.contentDisposition) {
      headers.set(
        "Content-Disposition",
        object.httpMetadata.contentDisposition,
      );
    }

    if (request.method === "HEAD") {
      return new Response(null, { status: 200, headers });
    }

    return new Response(object.body, { status: 200, headers });
  },
} satisfies ExportedHandler<Env>;
