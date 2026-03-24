using System.Net;

namespace MovieRentalApp.Middleware
{
    public class VideoStreamingMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly HashSet<string> VideoExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".mp4", ".webm", ".ogg", ".mkv", ".avi", ".mov"
            };

        public VideoStreamingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            var ext = Path.GetExtension(path);

            // ── Not a video request → pass through ────────────────
            if (!VideoExtensions.Contains(ext))
            {
                await _next(context);
                return;
            }

            // ── Resolve physical file path ─────────────────────────
            // URL example : /uploads/movies/movie_3_54b39981-d99e-438d-b1c4-3f4.mp4
            // Physical    : {WebRootPath}/uploads/movies/movie_3_54b39981-d99e-438d-b1c4-3f4.mp4
            var env = context.RequestServices
                             .GetRequiredService<IWebHostEnvironment>();

            var relativePath = path.TrimStart('/')
                                   .Replace('/', Path.DirectorySeparatorChar);

            var filePath = Path.Combine(env.WebRootPath, relativePath);

            // ── Debug log (you can remove after confirming it works) ──
            var logger = context.RequestServices
                                .GetRequiredService<ILogger<VideoStreamingMiddleware>>();
            logger.LogInformation(
                "[VideoStream] URL={Path} | File={File} | Exists={Exists}",
                path, filePath, File.Exists(filePath));

            if (!File.Exists(filePath))
            {
                // Let UseStaticFiles handle it (will 404 correctly)
                await _next(context);
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var totalBytes = fileInfo.Length;

            // ── Common headers ─────────────────────────────────────
            context.Response.Headers["Accept-Ranges"] = "bytes";
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Cache-Control"] = "public, max-age=3600";

            var contentType = GetContentType(ext);
            var rangeHeader = context.Request.Headers["Range"].ToString();

            // ── No Range header → serve full file ─────────────────
            if (string.IsNullOrEmpty(rangeHeader))
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = contentType;
                context.Response.ContentLength = totalBytes;

                await using var fullStream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 65536, useAsync: true);

                await fullStream.CopyToAsync(context.Response.Body);
                return;
            }

            // ── Parse "Range: bytes=start-end" ─────────────────────
            var rangeValue = rangeHeader.Replace("bytes=", "").Trim();
            var parts = rangeValue.Split('-');

            long start = 0;
            long end = totalBytes - 1;

            if (parts.Length >= 1 && long.TryParse(parts[0], out var parsedStart))
                start = parsedStart;

            if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]) &&
                long.TryParse(parts[1], out var parsedEnd))
                end = parsedEnd;
            else
                // Default: 2 MB chunk
                end = Math.Min(start + (2L * 1024 * 1024) - 1, totalBytes - 1);

            // ── Validate range ─────────────────────────────────────
            if (start > end || start >= totalBytes || start < 0)
            {
                context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                context.Response.Headers["Content-Range"] = $"bytes */{totalBytes}";
                return;
            }

            end = Math.Min(end, totalBytes - 1);
            var chunkSize = end - start + 1;

            // ── 206 Partial Content response ───────────────────────
            context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            context.Response.ContentType = contentType;
            context.Response.ContentLength = chunkSize;
            context.Response.Headers["Content-Range"] = $"bytes {start}-{end}/{totalBytes}";

            await using var stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, 65536, useAsync: true);

            stream.Seek(start, SeekOrigin.Begin);

            var buffer = new byte[65536];
            var remaining = chunkSize;

            while (remaining > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = await stream.ReadAsync(buffer, 0, toRead);
                if (read == 0) break;

                await context.Response.Body.WriteAsync(buffer, 0, read);
                remaining -= read;
            }
        }

        private static string GetContentType(string extension) =>
            extension.ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogg" => "video/ogg",
                ".mkv" => "video/x-matroska",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream"
            };
    }
}