namespace WebAPI.Middleweres
{
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogMiddleware> _logger;

        public LogMiddleware(RequestDelegate next, ILogger<LogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Iniciando {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            try
            {
                await _next(httpContext);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Finalizado {Method} {Path} com status {StatusCode} em {Elapsed}ms",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    httpContext.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
