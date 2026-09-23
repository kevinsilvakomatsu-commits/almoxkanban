using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using AlmoxKanban.Data;
using AlmoxKanban.Services;

// Compatibilidade de formato de datas (DateTime.Now) com PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Log no console para feedback visual imediato ao usuário
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configuração de URLs: lê a porta dinâmica na nuvem (Render/Docker) ou usa 5000 por padrão
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Banco de Dados PostgreSQL
// Lê a variável DATABASE_URL (fornecida automaticamente pelo Render) ou a ConnectionString do appsettings.json
var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "";

var connectionString = ConverterDatabaseUrlParaNpgsql(rawConnectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
});

// Injeção de Dependências dos Serviços
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITarefaService, TarefaService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IImportService, ImportService>();

// Autenticação por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "AlmoxKanban.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Razor Pages e Antiforgery
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Logout");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AuthorizeFolder("/Equipe", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Backup", "AdminOnly");
});

builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");

var app = builder.Build();

// Garantir pastas de dados e uploads
var dataDir = Path.Combine(app.Environment.ContentRootPath, "Data");
if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

var uploadsTarefas = Path.Combine(app.Environment.WebRootPath, "uploads", "tarefas");
var uploadsResolucoes = Path.Combine(app.Environment.WebRootPath, "uploads", "resolucoes");
var uploadsPerfis = Path.Combine(app.Environment.WebRootPath, "uploads", "perfis");
var templatesDir = Path.Combine(app.Environment.WebRootPath, "templates");

if (!Directory.Exists(uploadsTarefas)) Directory.CreateDirectory(uploadsTarefas);
if (!Directory.Exists(uploadsResolucoes)) Directory.CreateDirectory(uploadsResolucoes);
if (!Directory.Exists(uploadsPerfis)) Directory.CreateDirectory(uploadsPerfis);
if (!Directory.Exists(templatesDir)) Directory.CreateDirectory(templatesDir);

// Inicialização automática do banco e SeedData
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        SeedData.Initialize(context);

        var modeloPath = Path.Combine(templatesDir, "modelo_importacao.xlsx");
        if (!File.Exists(modeloPath))
        {
            var importService = services.GetRequiredService<IImportService>();
            var bytes = importService.GerarPlanilhaModeloUsuarios();
            File.WriteAllBytes(modeloPath, bytes);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[AlmoxKanban] Banco de dados e dados iniciais prontos!");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[AlmoxKanban] Erro na inicializacao do banco: {ex.Message}");
        Console.ResetColor();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware para forçar troca de senha caso flag esteja ativa
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var deveTrocar = context.User.FindFirst("DeveTrocarSenha")?.Value == "True";

        if (deveTrocar && !path.StartsWith("/alterarsenha") && !path.StartsWith("/logout") && !path.StartsWith("/css") && !path.StartsWith("/js") && !path.StartsWith("/lib"))
        {
            context.Response.Redirect("/AlterarSenha");
            return;
        }
    }
    await next();
});

app.MapRazorPages();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==========================================================");
Console.WriteLine("  SISTEMA ALMOXKANBAN INICIADO COM SUCESSO!");
Console.WriteLine($"  Porta: {port}");
Console.WriteLine("  Usuario Administrador: admin");
Console.WriteLine("  Senha Padrao:          Admin@123");
Console.WriteLine("==========================================================");
Console.ResetColor();

app.Run();

// Função auxiliar para converter URLs do padrão Render (postgres://...) para o formato Npgsql
static string ConverterDatabaseUrlParaNpgsql(string rawUrl)
{
    if (string.IsNullOrWhiteSpace(rawUrl)) return rawUrl;

    if (!rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return rawUrl;
    }

    try
    {
        var uri = new Uri(rawUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
    }
    catch
    {
        return rawUrl;
    }
}