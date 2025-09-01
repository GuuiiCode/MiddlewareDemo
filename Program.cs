var builder = WebApplication.CreateBuilder(args);

// ------------------------------- Services ------------------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCORS", policy =>
    {
        policy.WithOrigins("https://mywebsite.com") // Allow only your domain
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ------------------------------ Middlewares ------------------------------

// 1. Force HTTPS
app.UseHttpsRedirection();

// 2. Global error handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An unexpected error occurred. Please try again later!");
    });
});

// 3. logging (before and after)
app.Use(async (context, next) =>
{
    Console.WriteLine($"[LOG] Starting: {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"[LOG] Finished: {context.Response.StatusCode}");
});

// 4. Fake authentication (token in the header)
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault();

    if (token != "my-secret-token")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid or missing token");
        return;
    }

    // Mock authenticated user
    context.Items["User"] = "Guilherme";
    context.Items["Role"] = "Admin";

    await next();
});

// 5. Fake role-based authorization
app.Use(async (context, next) =>
{
    var role = context.Items["Role"] as string;

    if (role != "Admin")
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Access denied: the user is not an admin.");
        return;
    }
    await next();
});

// 6. CORS
app.UseCors("MyCORS");

// 7. Session
app.UseSession();
app.Use(async (context, next) =>
{
    var usuario = context.Items["User"] as string ?? "UnKnown";
    context.Session.SetString("User", usuario);
    Console.WriteLine($"[Session]: User saved in the session: {usuario}");
    await next();
});

// 8. Static files (wwwroot)
app.UseDefaultFiles(); // Automatically serves index.html
app.UseStaticFiles();

// 9. Last endpoint
app.MapGet("/", async context =>
{
    var usuario = context.Session.GetString("User");
    await context.Response.WriteAsync($"Welcome, {usuario}! Pipeline 2.1 is working");
});

app.Run();