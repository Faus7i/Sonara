namespace MusicRec.Catalog.DTOs;

/// <summary>
/// 导入曲目请求
/// </summary>
public record ImportTrackRequest(string SpotifyTrackId);

/// <summary>
/// 导入艺术家请求
/// </summary>
public record ImportArtistRequest(string SpotifyArtistId);
