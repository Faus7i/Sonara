using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicRec.AudioFeatures;
using MusicRec.Catalog;
using MusicRec.Identity;
using MusicRec.Infrastructure;
using MusicRec.Favorites;
using MusicRec.Playlists;
using MusicRec.Player;
using MusicRec.Search;
using MusicRec.Shared;
using MusicRec.Spotify;
using MusicRec.WebApi.Infrastructure;
using MusicRec.WebApi.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ───────────────────────────────────────
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ─── 数据库 (SQL Server) ──────────────────────────
builder.Services.AddDbContext<MusicRecDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ─── JWT 配置验证 ────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 16)
    throw new InvalidOperationException("Jwt:Secret 未配置或长度不足（至少 16 字符）");
if (string.IsNullOrEmpty(jwtIssuer))
    throw new InvalidOperationException("Jwt:Issuer 未配置");
if (string.IsNullOrEmpty(jwtAudience))
    throw new InvalidOperationException("Jwt:Audience 未配置");

// ─── JWT 认证 ─────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // 自定义 401/403 响应为统一 ApiResponse 格式（替代默认的纯文本/空体）
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    ApiResponse.Fail("未登录或登录已过期"),
                    JsonDefaults.CamelCaseOptions));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    ApiResponse.Fail("无权限访问该资源"),
                    JsonDefaults.CamelCaseOptions));
            }
        };
    });
builder.Services.AddAuthorization();

// ─── 控制器 & JSON 配置 ────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ─── Swagger ─────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─── 验证管道（全局注册一次，避免各模块重复注册导致多次执行）─
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ─── Spotify 基础设施 ────────────────────────────
builder.Services.AddSpotify();

// ─── 业务模块 ─────────────────────────────────────
builder.Services.AddIdentityModule();
builder.Services.AddCatalogModule();
builder.Services.AddAudioFeaturesModule();
builder.Services.AddSearchModule();
builder.Services.AddPlayerModule();
builder.Services.AddFavoritesModule();
builder.Services.AddPlaylistModule();

var app = builder.Build();

// ─── 中间件管道（顺序至关重要）────────────────────
app.UseSerilogRequestLogging();       // 1. 请求日志
app.UseMiddleware<GlobalExceptionMiddleware>(); // 2. 异常捕获（包裹后续所有中间件）

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();              // 3. 认证
app.UseAuthorization();               // 4. 授权
app.MapControllers();                 // 5. 路由

// ─── 自动应用 EF 迁移 ──────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MusicRecDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "数据库迁移失败，继续启动应用");
}

// ─── 启动 + Serilog 刷新 ──────────────────────────
try
{
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
