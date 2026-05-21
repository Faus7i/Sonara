using Mapster;
using MediatR;
using MusicRec.Player.DTOs;
using MusicRec.Player.Queries;
using MusicRec.Spotify;

namespace MusicRec.Player.Handlers;

/// <summary>
/// 获取可用设备列表处理器
/// </summary>
public class GetAvailableDevicesQueryHandler : IRequestHandler<GetAvailableDevicesQuery, IReadOnlyList<DeviceDto>>
{
    private readonly ISpotifyClient _spotify;

    public GetAvailableDevicesQueryHandler(ISpotifyClient spotify) => _spotify = spotify;

    public async Task<IReadOnlyList<DeviceDto>> Handle(GetAvailableDevicesQuery request, CancellationToken ct)
    {
        var devices = await _spotify.GetAvailableDevicesAsync(ct);
        // 防御：Spotify API 极端情况下可能返回 null
        return (devices ?? new()).Adapt<List<DeviceDto>>();
    }
}
