# 音频特征 API 迁移方案

> **状态**：方案已完成，代码待实施  
> **目标**：将音频特征获取从 Spotify 原生 API 切换为 RapidAPI 第三方 API  
> **原因**：Spotify 已废弃 Audio Features 端点，第三方 API 提供兼容替代方案

---

## 一、候选 API 对比

### 1.1 三维总览

| 维度 | 🥇 Spotify Extended | 🥈 Spotify Audio Features / Track Analysis | 🥉 SoundNet Track Analysis |
|------|---------------------|------------------------------------------|---------------------------|
| RapidAPI 标识 | `spotify-extended-audio-features-api` | `spotify-audio-features-track-analysis` | `track-analysis` |
| 基础 URL | `spotify-extended-audio-features-api.p.rapidapi.com` | `spotify-audio-features-track-analysis.p.rapidapi.com` | `track-analysis.p.rapidapi.com` |
| 提供商 | Musicae | — | SoundNet |
| 响应格式 | **Spotify 原生格式** (float/int, snake_case) | 自定义嵌套格式 (全部 string) | 自有格式 (int 0-100, camelCase) |
| 数据映射 | **无需映射** ✅ | 需解析 string→数值 + 嵌套结构 | 需 14 字段类型转换 |
| 单曲端点 | `GET /v1/audio-features/{id}` | `GET /tracks/spotify_audio_features?spotify_track_id=` | `GET /pktx/spotify/{trackID}` |
| 批量端点 | ✅ `GET /v1/audio-features?ids=` | ❌ 不支持 | ❌ 不支持 |
| 推荐端点 | ✅ `GET /v1/recommendations` | ❌ 不支持 | ❌ 不支持 |
| 相关艺术家 | ✅ `GET /v1/artists/{id}/related-artists` | ❌ 不支持 | ❌ 不支持 |
| 完整字段数 | 18（与 Spotify 一致） | 11（缺 duration_ms, time_signature 等） | 14（缺 time_signature, 多 camelot 等） |
| 字段值类型 | float/int（原生类型） | **全部 string**（需 Parse） | int/string 混合 |
| 响应中的 track ID | `"id"` = Spotify Track ID | `"spotify_track_id"` = Spotify Track ID | `"id"` = SoundNet 内部 ID（非 Spotify ID） |
| 延迟 | — | **659ms** | 3759ms |
| 免费限制 | 有配额 | **1 req/s** | 有配额 |
| PRO 月费 | **$9.99** | $49.99 | — |
| 认证方式 | RapidAPI Key Header | RapidAPI Key Header | RapidAPI Key Header |

### 1.2 响应格式对比

**🥇 Spotify Extended（与 Spotify 原生完全一致，18 字段）：**
```json
{
  "acousticness": 0.0194,
  "danceability": 0.613,
  "duration_ms": 320357,
  "energy": 0.697,
  "id": "0DiWol3AO6WpXZgp0goxAV",
  "instrumentalness": 0,
  "key": 2,
  "liveness": 0.332,
  "loudness": -8.618,
  "mode": 1,
  "speechiness": 0.133,
  "tempo": 122.746,
  "time_signature": 4,
  "valence": 0.476,
  "type": "audio_features",
  "uri": "spotify:track:0DiWol3AO6WpXZgp0goxAV"
}
```

**🥈 Spotify Audio Features / Track Analysis（嵌套结构 + 全部 string，11 字段）：**
```json
{
  "success": "true",
  "message": "Data Retrieved.",
  "spotify_track_id": "6ho0GyrWZN3mhi9zVRW7xi",
  "isrc": "CA5KR1821202",
  "audio_features": {
    "danceability": "0.76",
    "energy": "0.964",
    "key": "D",
    "loudness": "-5.844",
    "mode": "1.0",
    "speechiness": "0.0577",
    "acousticness": "0.0018",
    "instrumentalness": "0.703",
    "liveness": "0.0975",
    "valence": "0.643",
    "tempo": "125.003"
  }
}
```
> ⚠️ 缺失：`duration_ms`、`time_signature`；所有值为 string 类型需 Parse；响应嵌套在 `audio_features` 对象中

**🥉 SoundNet（自有格式，int 0-100 范围）：**
```json
{
  "id": "a396e5ef3e08c870041b67c0d0e7863d",
  "key": "C",
  "mode": "major",
  "tempo": 115,
  "duration": "2:27",
  "energy": 56,
  "danceability": 81,
  "happiness": 97,
  "acousticness": 16,
  "instrumentalness": 0,
  "liveness": 5,
  "speechiness": 4,
  "loudness": "-5 dB"
}
```

### 1.3 结论：选择 Spotify Extended API

**核心理由**：响应格式与现有 `AudioFeaturesObject` 模型 **100% 兼容**，零映射改动。另外两个 API 都需大量值转换代码。

| 对比维度 | Spotify Extended | Audio Features / Track Analysis | SoundNet |
|---------|:---:|:---:|:---:|
| 可直接反序列化为 `AudioFeaturesObject` | ✅ 是 | ❌ 否（嵌套+全string） | ❌ 否（完全不同格式） |
| 保持 `AudioFeatureMapping` 不变 | ✅ 是 | ❌ 否 | ❌ 否 |
| 保持 Handler 分批逻辑不变 | ✅ 是（有批量端点） | ❌ 无批量端点 | ❌ 无批量端点 |
| 零字段缺失 | ✅ 18/18 | ❌ 缺 2 个 | ❌ 缺 2 个 |
| 后续支持推荐功能 | ✅ 内置 | ❌ 无 | ❌ 无 |
| 性价比 | 最佳 ($9.99) | 差 ($49.99 + 1req/s) | 一般 |

---

## 二、Spotify Extended API 详细说明（主方案）

### 2.1 基本信息

| 项目 | 内容 |
|------|------|
| API 名称 | Spotify Extended Audio Features API (by Musicae) |
| RapidAPI 页面 | `spotify-extended-audio-features-api` |
| 基础 URL | `https://spotify-extended-audio-features-api.p.rapidapi.com` |
| 认证方式 | RapidAPI Key（`x-rapidapi-key` + `x-rapidapi-host` Header） |
| 延迟 | p95 优化，sub-second 中位响应时间（预热后，Redis 缓存） |
| 缓存 | Redis-backed，多亿曲目库 |

### 2.2 端点列表

#### 2.2.1 单曲音频特征

```
GET /v1/audio-features/{spotifyTrackId}
```

**请求 Headers：**
```
x-rapidapi-key: YOUR_RAPIDAPI_KEY
x-rapidapi-host: spotify-extended-audio-features-api.p.rapidapi.com
```

**响应**：标准 Spotify `AudioFeaturesObject` 格式（见上方 JSON 示例）

**HTTP 状态码处理**：
- 200 → 正常返回
- 404 → 曲目未分析（返回 null）
- 429 → 限流（Polly 重试）
- 5xx → 服务端错误（Polly 重试）

#### 2.2.2 批量音频特征

```
GET /v1/audio-features?ids={commaSeparatedIds}
```

限制：每次最多 100 个 ID（与 Spotify 原生一致）

**响应**：
```json
{
  "audio_features": [
    { "acousticness": 0.0194, "danceability": 0.613, ... },
    null,
    { "acousticness": 0.542, "danceability": 0.711, ... }
  ]
}
```

对未分析的曲目，数组中对应位置为 `null`，需过滤。

#### 2.2.3 推荐（附加能力，Phase 4-5 使用）

```
GET /v1/recommendations?seed_artists=&seed_tracks=&seed_genres=&limit=&target_energy=...
```

参数与 Spotify 推荐 API 完全一致（`seed_*`、`limit`、`target_*`、`min_*`、`max_*`）。本模块暂不实现，方案文档保留接口信息。

#### 2.2.4 相关艺术家（附加能力）

```
GET /v1/artists/{spotifyArtistId}/related-artists
```

### 2.3 与 Spotify 原生 API 的关键差异

| 差异点 | Spotify 原生 | Spotify Extended |
|--------|-------------|-----------------|
| Base URL | `api.spotify.com/v1` | `spotify-extended-audio-features-api.p.rapidapi.com` |
| 认证头 | `Authorization: Bearer {token}` | `x-rapidapi-key` + `x-rapidapi-host` |
| Token 管理 | OAuth 2.0 Client Credentials（3600s 过期） | API Key（永不过期） |
| 是否需 Spotify 账号 | 是（需注册应用获取 ClientId/Secret） | 否 |
| 推荐端点 | 已废弃 | 可用 ✅ |
| 相关艺术家端点 | 已废弃 | 可用 ✅ |

---

## 三、改动方案（主方案：Spotify Extended API）

### 3.1 核心思路

1. **零数据映射改动** — 响应格式完全兼容现有 `AudioFeaturesObject` 模型，`AudioFeatureMapping` 不变
2. **零业务逻辑改动** — Handler 中调用方法的签名和返回值类型不变，只改注入的接口
3. **新建薄层** — 创建 `MusicRec.SpotifyExtended` 项目，仅含 HTTP 客户端 + API Key 认证
4. **保留现有 Spotify 基础设施** — 搜索/曲目详情/艺术家/专辑/播放控制继续用 Spotify 原生 API

### 3.2 架构对比

```
改动前：
  AudioFeatures 模块
    └── ISpotifyClient (注入 SpotifyClient)
          └── Spotify OAuth → api.spotify.com/v1/audio-features/

改动后：
  AudioFeatures 模块
    └── ISpotifyExtendedClient (注入 SpotifyExtendedClient)
          └── RapidAPI Key → spotify-extended-audio-features-api.p.rapidapi.com/v1/audio-features/

  Spotify 基础设施（不变）
    └── ISpotifyClient → 搜索/曲目详情/艺术家/专辑/播放控制
```

### 3.3 新建文件清单

```
src/Infrastructure/SpotifyExtended/          # 新建项目
├── MusicRec.SpotifyExtended.csproj          # 项目文件
├── ISpotifyExtendedClient.cs                # 接口（2 个方法）
├── SpotifyExtendedClient.cs                 # 实现
├── SpotifyExtendedOptions.cs                # 配置绑定
├── DependencyInjection.cs                   # DI 注册 + Polly 重试
```

### 3.4 ISpotifyExtendedClient 接口

```csharp
namespace MusicRec.SpotifyExtended;

public interface ISpotifyExtendedClient
{
    /// <summary>获取单曲音频特征</summary>
    Task<AudioFeaturesObject?> GetAudioFeaturesAsync(string spotifyId, CancellationToken ct = default);

    /// <summary>批量获取音频特征（最多 100 个）</summary>
    Task<List<AudioFeaturesObject>> GetAudioFeaturesBatchAsync(IReadOnlyList<string> spotifyIds, CancellationToken ct = default);
}
```

方法签名与 `ISpotifyClient` 中现有的音频特征方法完全一致，确保 Handler 调用方式不变。

### 3.5 SpotifyExtendedClient 实现

```csharp
namespace MusicRec.SpotifyExtended;

public class SpotifyExtendedClient : ISpotifyExtendedClient
{
    private readonly HttpClient _http;

    public SpotifyExtendedClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AudioFeaturesObject?> GetAudioFeaturesAsync(string spotifyId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"v1/audio-features/{spotifyId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AudioFeaturesObject>(SpotifyJsonDefaults.Options, ct);
    }

    public async Task<List<AudioFeaturesObject>> GetAudioFeaturesBatchAsync(
        IReadOnlyList<string> spotifyIds, CancellationToken ct = default)
    {
        if (spotifyIds.Count == 0) return new List<AudioFeaturesObject>();
        var ids = string.Join(",", spotifyIds);
        var response = await _http.GetAsync($"v1/audio-features?ids={ids}", ct);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<SeveralAudioFeaturesResponse>(SpotifyJsonDefaults.Options, ct))!;
        return result.AudioFeatures.Where(af => af is not null).ToList();
    }
}
```

关键点：
- URL 路径与 Spotify API 一致（`v1/audio-features/...`），因为该 API 完全镜像 Spotify 路径结构
- 复用 `SpotifyJsonDefaults.Options`（SnakeCaseLower 策略），因为响应字段名与 Spotify 一致（snake_case）
- 复用 `AudioFeaturesObject` 和 `SeveralAudioFeaturesResponse` 模型（来自 `MusicRec.Spotify.Models`）
- 404 处理逻辑不变（返回 null）

### 3.6 DI 注册

```csharp
// DependencyInjection.cs
public static class DependencyInjection
{
    private const int MaxRetryAttempts = 5;
    private const double BackoffBaseSeconds = 2;
    private const int JitterMaxMs = 1000;
    private const int HandlerLifetimeMinutes = 5;

    public static IServiceCollection AddSpotifyExtended(this IServiceCollection services)
    {
        services.AddOptions<SpotifyExtendedOptions>()
            .BindConfiguration(SpotifyExtendedOptions.SectionName);

        services.AddHttpClient<ISpotifyExtendedClient, SpotifyExtendedClient>(client =>
        {
            client.BaseAddress = new Uri("https://spotify-extended-audio-features-api.p.rapidapi.com/");
        })
        .ConfigureHttpClient((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SpotifyExtendedOptions>>().Value;
            client.DefaultRequestHeaders.Add("x-rapidapi-key", options.ApiKey);
            client.DefaultRequestHeaders.Add("x-rapidapi-host", options.ApiHost);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .SetHandlerLifetime(TimeSpan.FromMinutes(HandlerLifetimeMinutes));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(BackoffBaseSeconds, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, JitterMaxMs))
            );
    }
}
```

注意：API Key 是固定值，放入 `DefaultRequestHeaders` 无多线程竞争问题（与不固定的 Bearer Token 不同，后者必须用 DelegatingHandler）。

### 3.7 配置文件

```csharp
// SpotifyExtendedOptions.cs
public class SpotifyExtendedOptions
{
    public const string SectionName = "SpotifyExtended";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiHost { get; set; } = string.Empty;
}
```

```json
// appsettings.json
{
  "SpotifyExtended": {
    "ApiKey": "YOUR_RAPIDAPI_KEY",
    "ApiHost": "spotify-extended-audio-features-api.p.rapidapi.com"
  }
}
```

---

## 四、修改文件汇总（主方案）

### 4.1 新建（5 个文件）

| 文件 | 说明 |
|------|------|
| `src/Infrastructure/SpotifyExtended/MusicRec.SpotifyExtended.csproj` | 项目文件，引用 `MusicRec.Shared` + `MusicRec.Spotify` |
| `src/Infrastructure/SpotifyExtended/ISpotifyExtendedClient.cs` | 接口（2 个方法） |
| `src/Infrastructure/SpotifyExtended/SpotifyExtendedClient.cs` | 实现 |
| `src/Infrastructure/SpotifyExtended/SpotifyExtendedOptions.cs` | 配置 Options |
| `src/Infrastructure/SpotifyExtended/DependencyInjection.cs` | DI 注册 + Polly |

### 4.2 修改（6 个文件）

| 文件 | 改动 |
|------|------|
| `MusicRec.sln` | 添加 `MusicRec.SpotifyExtended` 项目 |
| `src/Web/MusicRec.WebApi/Program.cs` | 添加 `builder.Services.AddSpotifyExtended();` |
| `src/Web/MusicRec.WebApi/appsettings.json` | 添加 `SpotifyExtended` 配置节 |
| `src/Modules/AudioFeatures/MusicRec.AudioFeatures.csproj` | 项目引用 `MusicRec.Spotify` → `MusicRec.SpotifyExtended` |
| `src/Modules/AudioFeatures/Handlers/GetAudioFeaturesQueryHandler.cs` | `ISpotifyClient` → `ISpotifyExtendedClient` |
| `src/Modules/AudioFeatures/Handlers/ImportAudioFeaturesCommandHandler.cs` | `ISpotifyClient` → `ISpotifyExtendedClient` |

### 4.3 可选清理（Spotify 端）

| 文件 | 改动 |
|------|------|
| `src/Infrastructure/Spotify/ISpotifyClient.cs` | 删除 `GetAudioFeaturesAsync`、`GetAudioFeaturesBatchAsync` |
| `src/Infrastructure/Spotify/SpotifyClient.cs` | 删除对应实现（约 20 行） |

`AudioFeaturesObject` 和 `SeveralAudioFeaturesResponse` 模型保留在 `MusicRec.Spotify.Models` 中，因为 `MusicRec.SpotifyExtended` 引用它们（响应格式相同）。

---

## 五、完整实现步骤（主方案）

### 步骤 1：创建 `MusicRec.SpotifyExtended` 项目

创建 `src/Infrastructure/SpotifyExtended/` 目录，新建 5 个文件：

- `MusicRec.SpotifyExtended.csproj` — 参照 `MusicRec.Spotify.csproj`，引用 `MusicRec.Shared` + `MusicRec.Spotify`（仅复用 Models）
- `SpotifyExtendedOptions.cs` — 配置类
- `ISpotifyExtendedClient.cs` — 接口
- `SpotifyExtendedClient.cs` — 实现
- `DependencyInjection.cs` — DI 扩展方法

### 步骤 2：修改 AudioFeatures 模块

1. **`MusicRec.AudioFeatures.csproj`**
   - 删除 `<ProjectReference>` 指向 `MusicRec.Spotify`
   - 添加 `<ProjectReference>` 指向 `MusicRec.SpotifyExtended`

2. **`GetAudioFeaturesQueryHandler.cs`**
   - 注入 `ISpotifyExtendedClient` 替代 `ISpotifyClient`
   - 调用方式不变：`_client.GetAudioFeaturesAsync(spotifyId, ct)`

3. **`ImportAudioFeaturesCommandHandler.cs`**
   - 注入 `ISpotifyExtendedClient` 替代 `ISpotifyClient`
   - 调用方式不变：`_client.GetAudioFeaturesBatchAsync(batch, ct)`
   - 分批逻辑完全不变

### 步骤 3：注册到 Program.cs

```csharp
// 在 builder.Services.AddSpotify(); 之后添加
builder.Services.AddSpotifyExtended();
```

### 步骤 4：添加配置

在 `appsettings.json` 中添加 `SpotifyExtended` 配置节，填入 RapidAPI Key。

### 步骤 5：更新解决方案

在 `MusicRec.sln` 中添加 `MusicRec.SpotifyExtended.csproj`。

### 步骤 6：构建验证

```bash
dotnet restore
dotnet build
```

---

## 六、不改动的部分（主方案）

以下**完全不动**：

- 数据库 Schema（`TrackAudioFeatures` 表）
- `TrackAudioFeature` 实体（14 字段）
- `AudioFeaturesDto` 定义
- `TrackAudioFeatureConfiguration`（EF 配置）
- `AudioFeatureMapping`（Mapster 映射，`AudioFeaturesObject → TrackAudioFeature` 保持不变）
- `GetAudioFeaturesQuery` / `ImportAudioFeaturesCommand`（命令/查询定义）
- `ImportAudioFeaturesCommandValidator`（验证器，100 上限不变）
- Controller 层
- Catalog、Search、Player、Favorites、Playlist 所有模块
- Spotify 认证体系（`SpotifyAuthHandler`、`SpotifyTokenStore`）
- `ToVector()` 和推荐算法逻辑

---

## 七、备选方案 A：Spotify Audio Features / Track Analysis API

> 当 Spotify Extended 不可用时，此 API 是第二选择（值范围与 Spotify 一致，但格式不同）。

### 7.1 基本信息

| 项目 | 内容 |
|------|------|
| API 名称 | Spotify Audio Features / Track Analysis |
| RapidAPI 标识 | `spotify-audio-features-track-analysis` |
| 基础 URL | `https://spotify-audio-features-track-analysis.p.rapidapi.com` |
| 端点 | `GET /tracks/spotify_audio_features?spotify_track_id={id}&isrc={isrc}` |
| 免费限制 | **1 request/second**（硬限制） |
| PRO 月费 | **$49.99** |
| 延迟 | 659ms（三者中最快） |

### 7.2 请求方式

**C# 示例：**
```csharp
var client = new HttpClient();
var request = new HttpRequestMessage
{
    Method = HttpMethod.Get,
    RequestUri = new Uri("https://spotify-audio-features-track-analysis.p.rapidapi.com/tracks/spotify_audio_features?spotify_track_id=6ho0GyrWZN3mhi9zVRW7xi"),
    Headers =
    {
        { "x-rapidapi-key", "YOUR_API_KEY" },
        { "x-rapidapi-host", "spotify-audio-features-track-analysis.p.rapidapi.com" },
    },
};
using var response = await client.SendAsync(request);
response.EnsureSuccessStatusCode();
var body = await response.Content.ReadAsStringAsync();
```

### 7.3 响应结构

```json
{
  "success": "true",
  "message": "Data Retrieved.",
  "spotify_track_id": "6ho0GyrWZN3mhi9zVRW7xi",
  "isrc": "CA5KR1821202",
  "audio_features": {
    "danceability": "0.76",
    "energy": "0.964",
    "key": "D",
    "loudness": "-5.844",
    "mode": "1.0",
    "speechiness": "0.0577",
    "acousticness": "0.0018",
    "instrumentalness": "0.703",
    "liveness": "0.0975",
    "valence": "0.643",
    "tempo": "125.003"
  }
}
```

### 7.4 字段映射：响应 → TrackAudioFeature

| 实体字段 | 响应来源（嵌套路径） | 原始类型 | 转换逻辑 |
|----------|---------------------|---------|---------|
| `Acousticness` | `audio_features.acousticness` | `string` "0.0018" | `float.Parse()` |
| `Danceability` | `audio_features.danceability` | `string` "0.76" | `float.Parse()` |
| `Energy` | `audio_features.energy` | `string` "0.964" | `float.Parse()` |
| `Instrumentalness` | `audio_features.instrumentalness` | `string` "0.703" | `float.Parse()` |
| `Key` | `audio_features.key` | `string` "D" | 音名映射表 → int 0-11 |
| `Liveness` | `audio_features.liveness` | `string` "0.0975" | `float.Parse()` |
| `Loudness` | `audio_features.loudness` | `string` "-5.844" | `float.Parse()` |
| `Mode` | `audio_features.mode` | `string` "1.0" | `float.Parse()` → int (0=minor, 1=major) |
| `Speechiness` | `audio_features.speechiness` | `string` "0.0577" | `float.Parse()` |
| `Tempo` | `audio_features.tempo` | `string` "125.003" | `float.Parse()` |
| `Valence` | `audio_features.valence` | `string` "0.643" | `float.Parse()` |
| `DurationMs` | **缺失** | — | 默认 `0` |
| `TimeSignature` | **缺失** | — | 默认 `4` |
| `SpotifyTrackId` | `spotify_track_id`（顶层） | `string` | 直接赋值 |
| `Id` | — | — | DB 自动生成 |

### 7.5 与主方案的额外改动

相比 Spotify Extended 方案，此 API **额外**需要：

| 文件 | 额外改动 |
|------|---------|
| `src/Infrastructure/.../Models/TrackAnalysisResponse.cs` | 新建嵌套响应模型（顶层 + `audio_features` 子对象，全部 string 类型） |
| `src/Modules/AudioFeatures/AudioFeatureMapping.cs` | 重写全部映射（11 个 `float.Parse()` + Key 音名映射） |
| `src/Modules/AudioFeatures/Handlers/ImportAudioFeaturesCommandHandler.cs` | 无批量端点 → 重写为 `SemaphoreSlim` + `Task.WhenAll` 并发，且限 1 req/s |
| `src/Modules/AudioFeatures/Validators/ImportAudioFeaturesCommandValidator.cs` | 下调上限（1 req/s 限制下大批量不现实） |

### 7.6 额外风险

| 风险 | 说明 |
|------|------|
| 全部 string 类型 | `float.Parse()` 依赖 CultureInfo，需指定 `CultureInfo.InvariantCulture` |
| 1 req/s 硬限制 | 批量导入极慢（100 首需 100 秒），不适合大批量场景 |
| `isrc` 参数 | 非必填但 URL 中包含，不确定是否影响查询准确性 |
| 价格高 5 倍 | PRO $49.99 vs Spotify Extended PRO $9.99 |
| Key 字符串映射 | "D"、"G#" 等需完整覆盖 12 音名 |

---

## 八、备选方案 B：SoundNet Track Analysis API

> 前两个 API 均不可用时的最后备选。格式差异最大，改动量最大。

### 8.1 基本信息

| 项目 | 内容 |
|------|------|
| API 名称 | Track Analysis API by SoundNet |
| RapidAPI 标识 | `track-analysis`（soundnet-soundnet-default） |
| 基础 URL | `https://track-analysis.p.rapidapi.com` |
| 端点 | `GET /pktx/spotify/{trackID}` |
| 批量 | ❌ 不支持 |
| 延迟 | 3759ms（三者中最慢） |

### 8.2 响应格式

```json
{
  "id": "a396e5ef3e08c870041b67c0d0e7863d",
  "name": "Respect",
  "album": "I Never Loved a Man the Way I Love You",
  "key": "C",
  "mode": "major",
  "camelot": "8B",
  "tempo": 115,
  "duration": "2:27",
  "popularity": 77,
  "energy": 56,
  "danceability": 81,
  "happiness": 97,
  "acousticness": 16,
  "instrumentalness": 0,
  "liveness": 5,
  "speechiness": 4,
  "loudness": "-5 dB"
}
```

### 8.3 字段映射差异

所有 0-1 范围字段从 float 变为 **int 0-100**，需 `÷100` 转换。Key 为字符串需映射表。Duration 为 "m:ss" 需解析。`time_signature` 缺失。`happiness` 对应 `valence`。响应 `id` 非 Spotify Track ID（需从请求参数传入）。

### 8.4 与主方案的额外改动

相比 Spotify Extended 方案，需额外改动：

| 文件 | 额外改动 |
|------|---------|
| 新建响应模型（SoundNet 自有格式，16 字段） | 类型完全不同（int 替代 float，string 替代 int） |
| `AudioFeatureMapping.cs` | 重写全部 14 字段映射 + 3 个辅助方法 |
| `ImportAudioFeaturesCommandHandler.cs` | 去分批逻辑 → 并发 + 限流 |
| `ImportAudioFeaturesCommandValidator.cs` | 调整上限 |

---

## 九、API Key 与配置管理

### 9.1 配置节设计（通用模式）

三种 API 使用统一的配置结构，仅 `ApiHost` 不同：

```json
// appsettings.json — 主方案（Spotify Extended）
{
  "SpotifyExtended": {
    "ApiKey": "YOUR_RAPIDAPI_KEY",
    "ApiHost": "spotify-extended-audio-features-api.p.rapidapi.com"
  }
}
```

### 9.2 安全注意事项

- `ApiKey` 不提交到 Git — 通过 `appsettings.Development.json` 或 `dotnet user-secrets` 管理
- 生产环境通过环境变量或密钥管理服务注入
- RapidAPI Key 与订阅计划绑定，月付到期后 API 返回 401

### 9.3 各 API 的限制对比

| 限制 | Spotify Extended | Audio Features / Track Analysis | SoundNet |
|------|:---:|:---:|:---:|
| 免费调用频率 | 有配额（Dashboard 查看） | **1 req/s** 硬限制 | 有配额 |
| 批量端点 | ✅ 100/次 | ❌ 无 | ❌ 无 |
| PRO 价格 | $9.99/mo | $49.99/mo | — |

---

## 十、扩展能力（Phase 4-5 预备）

### 10.1 推荐端点

```
GET /v1/recommendations
  ?seed_artists=4tZwfgrHOc3mvqYlEYSvVi
  &seed_tracks=0DiWol3AO6WpXZgp0goxAV
  &limit=10
  &target_energy=0.7
  &target_danceability=0.6
```

参数与 Spotify 推荐 API 完全一致（`seed_*`、`limit`、`target_*`、`min_*`、`max_*`）。

### 10.2 相关艺术家端点

```
GET /v1/artists/{spotifyArtistId}/related-artists
```

### 10.3 架构扩展路径

当 Phase 4-5 需要实现推荐功能时，只需在 `ISpotifyExtendedClient` 接口上添加两个方法即可，无需新建项目或修改现有模块依赖：

```csharp
// 后续扩展（本次不实现）
Task<RecommendationsResponse> GetRecommendationsAsync(...);
Task<List<ArtistObject>> GetRelatedArtistsAsync(string artistId, ...);
```
