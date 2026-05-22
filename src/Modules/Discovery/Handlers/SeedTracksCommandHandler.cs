using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicRec.Catalog.Entities;
using MusicRec.Discovery.Commands;
using MusicRec.Infrastructure;

namespace MusicRec.Discovery.Handlers;

/// <summary>
/// 种子曲目生成处理器 — 为探索页面生成覆盖 23 个流派的模拟曲目数据
/// </summary>
/// <remarks>
/// 幂等设计：所有 SpotifyId 使用 "seed_" 前缀，插入前检查存在性，
/// 重复调用返回 TracksCreated = 0。
/// 批量插入 500 条/批以控制 EF ChangeTracker 内存。
/// </remarks>
public class SeedTracksCommandHandler : IRequestHandler<SeedTracksCommand, SeedTracksResult>
{
    private readonly MusicRecDbContext _db;
    private readonly ILogger<SeedTracksCommandHandler> _logger;

    public SeedTracksCommandHandler(MusicRecDbContext db, ILogger<SeedTracksCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SeedTracksResult> Handle(SeedTracksCommand request, CancellationToken ct)
    {
        var genresCreated = await CreateGenres(ct);
        var artistsCreated = await CreateArtists(ct);
        var albumsCreated = await CreateAlbums(ct);
        var tracksCreated = await CreateTracks(ct);

        _logger.LogInformation(
            "种子曲目生成完成：{Genres} 流派, {Artists} 艺术家, {Albums} 专辑, {Tracks} 曲目",
            genresCreated, artistsCreated, albumsCreated, tracksCreated);

        return new SeedTracksResult(genresCreated, artistsCreated, albumsCreated, tracksCreated);
    }

    // ─── Phase 1: 流派 ────────────────────────────────

    private async Task<int> CreateGenres(CancellationToken ct)
    {
        var names = GenreNames;
        var existing = await _db.Set<Genre>()
            .Where(g => names.Contains(g.Name))
            .Select(g => g.Name)
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existing);

        var toAdd = names.Where(n => !existingSet.Contains(n))
            .Select(n => new Genre { Id = Guid.NewGuid(), Name = n })
            .ToList();

        if (toAdd.Count > 0)
        {
            _db.Set<Genre>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
        return toAdd.Count;
    }

    // ─── Phase 2: 艺术家 ──────────────────────────────

    private async Task<int> CreateArtists(CancellationToken ct)
    {
        var defs = ArtistDefs;
        var spotifyIds = defs.Select(d => ArtistSpotifyId(d.Name)).ToHashSet();
        var existingIds = await _db.Set<Artist>()
            .Where(a => spotifyIds.Contains(a.SpotifyArtistId))
            .Select(a => a.SpotifyArtistId)
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existingIds);

        var toAdd = new List<Artist>();
        foreach (var d in defs)
        {
            var sid = ArtistSpotifyId(d.Name);
            if (!existingSet.Contains(sid))
            {
                toAdd.Add(new Artist
                {
                    Id = Guid.NewGuid(),
                    SpotifyArtistId = sid,
                    Name = d.Name,
                    Genres = d.Genres,
                    Popularity = d.Popularity,
                    ImageUrl = null
                });
            }
        }

        if (toAdd.Count > 0)
        {
            _db.Set<Artist>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
        return toAdd.Count;
    }

    // ─── Phase 3: 专辑 ────────────────────────────────

    private async Task<int> CreateAlbums(CancellationToken ct)
    {
        var defs = AlbumDefs;
        var spotifyIds = defs.Select(d => AlbumSpotifyId(d.Name)).ToHashSet();
        var existingIds = await _db.Set<Album>()
            .Where(a => spotifyIds.Contains(a.SpotifyAlbumId))
            .Select(a => a.SpotifyAlbumId)
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existingIds);

        var toAdd = new List<Album>();
        foreach (var d in defs)
        {
            var sid = AlbumSpotifyId(d.Name);
            if (!existingSet.Contains(sid))
            {
                toAdd.Add(new Album
                {
                    Id = Guid.NewGuid(),
                    SpotifyAlbumId = sid,
                    Name = d.Name,
                    ReleaseDate = d.Year,
                    CoverImageUrl = null,
                    AlbumType = "album",
                    TotalTracks = d.TotalTracks
                });
            }
        }

        if (toAdd.Count > 0)
        {
            _db.Set<Album>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
        return toAdd.Count;
    }

    // ─── Phase 4 & 5: 曲目 + 关联 ──────────────────────

    private async Task<int> CreateTracks(CancellationToken ct)
    {
        // 加载所有已有实体（用于 FK 映射）
        var genres = await _db.Set<Genre>().ToDictionaryAsync(g => g.Name, ct);
        var artists = await _db.Set<Artist>().ToDictionaryAsync(a => a.SpotifyArtistId, ct);
        var albums = await _db.Set<Album>().ToDictionaryAsync(a => a.SpotifyAlbumId, ct);

        // 检查已存在的种子曲目
        var existingSeedIds = await _db.Set<Track>()
            .Where(t => t.SpotifyTrackId.StartsWith("seed_track_"))
            .Select(t => t.SpotifyTrackId)
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existingSeedIds);

        var tracks = new List<Track>();
        var trackArtists = new List<TrackArtist>();
        var trackGenres = new List<TrackGenre>();
        var rng = new Random(42); // 固定种子 → 确定性随机

        foreach (var albumDef in AlbumDefs)
        {
            var albumSid = AlbumSpotifyId(albumDef.Name);
            if (!albums.TryGetValue(albumSid, out var album)) continue;

            var trackNames = GetTrackNames(albumDef.Name);
            var artistNames = albumDef.ArtistNames.Split(',');
            var genreName = albumDef.Genre;

            foreach (var trackName in trackNames)
            {
                var trackSid = TrackSpotifyId(trackName, albumDef.Name);

                if (existingSet.Contains(trackSid)) continue;

                var track = new Track
                {
                    Id = Guid.NewGuid(),
                    SpotifyTrackId = trackSid,
                    Name = trackName,
                    AlbumId = album.Id,
                    DurationMs = rng.Next(180_000, 360_000),
                    Popularity = rng.Next(20, 96),
                    ReleaseDate = album.ReleaseDate,
                    CoverImageUrl = null
                };
                tracks.Add(track);

                // 艺术家关联
                foreach (var aName in artistNames)
                {
                    var aSid = ArtistSpotifyId(aName.Trim());
                    if (artists.TryGetValue(aSid, out var artist))
                        trackArtists.Add(new TrackArtist { TrackId = track.Id, ArtistId = artist.Id });
                }

                // 流派关联
                if (genres.TryGetValue(genreName, out var genre))
                    trackGenres.Add(new TrackGenre { TrackId = track.Id, GenreId = genre.Id });
                // 部分曲目附加第二流派
                var secondaryGenre = GetSecondaryGenre(genreName, rng);
                if (secondaryGenre is not null && genres.TryGetValue(secondaryGenre, out var sg))
                    trackGenres.Add(new TrackGenre { TrackId = track.Id, GenreId = sg.Id });
            }
        }

        if (tracks.Count == 0)
            return 0;

        // 批量插入
        const int batchSize = 500;
        var created = 0;
        for (var i = 0; i < tracks.Count; i += batchSize)
        {
            var tBatch = tracks.Skip(i).Take(batchSize).ToList();
            _db.Set<Track>().AddRange(tBatch);
            await _db.SaveChangesAsync(ct);
            created += tBatch.Count;
        }

        // 插入关联
        if (trackArtists.Count > 0)
        {
            _db.Set<TrackArtist>().AddRange(trackArtists);
            await _db.SaveChangesAsync(ct);
        }
        if (trackGenres.Count > 0)
        {
            _db.Set<TrackGenre>().AddRange(trackGenres);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("种子曲目：创建 {Tracks} 首, {TA} 条艺术家关联, {TG} 条流派关联",
            created, trackArtists.Count, trackGenres.Count);

        return created;
    }

    // ─── ID 生成 ───────────────────────────────────────

    private static string ArtistSpotifyId(string name)
        => "seed_artist_" + name.Replace(" ", "_").Replace("&", "").ToLowerInvariant();

    private static string AlbumSpotifyId(string name)
        => "seed_album_" + name.Replace(" ", "_").Replace("'", "").ToLowerInvariant();

    private static string TrackSpotifyId(string trackName, string albumName)
        => "seed_track_" + (trackName + "_" + albumName).Replace(" ", "_").Replace("'", "")
            .Replace(",", "").Replace(".", "").ToLowerInvariant();

    private static string? GetSecondaryGenre(string primary, Random rng)
    {
        if (rng.NextDouble() > 0.4) return null;  // 仅 40% 曲目有第二流派
        var others = GenreNames.Where(g => g != primary).ToList();
        return others[rng.Next(others.Count)];
    }

    // ─── 内联数据 ─────────────────────────────────────

    private static readonly string[] GenreNames =
    [
        "electronic", "edm", "house", "techno", "dance", "pop", "rock", "metal",
        "hip hop", "rap", "r&b", "jazz", "classical", "folk", "country", "indie",
        "ambient", "blues", "punk", "reggae", "funk", "soul", "latin"
    ];

    private record ArtistDef(string Name, string Genres, int Popularity);

    private static readonly ArtistDef[] ArtistDefs =
    [
        // 电子类
        new("Neon Pulse", "electronic,house", 82),
        new("Digital Horizon", "electronic,ambient", 60),
        new("Circuit Waves", "edm,dance", 78),
        new("Bass Reactor", "edm,techno", 71),
        new("Deep Sequence", "house,electronic", 69),
        new("Void Frequency", "techno,electronic", 65),
        new("Cosmic Dance", "dance,pop", 75),
        // 流行/摇滚/金属
        new("Luna Ray", "pop,dance", 85),
        new("The Velvet Notes", "pop,indie", 72),
        new("Crimson Tides", "rock", 78),
        new("Stone Pilgrims", "rock,blues", 65),
        new("Iron Requiem", "metal,rock", 70),
        new("Black Horizon", "metal", 62),
        // 嘻哈
        new("Urban Flow", "hip hop,rap", 76),
        new("MC Skyline", "hip hop", 68),
        new("Lyric Blaze", "rap,hip hop", 73),
        // R&B / 爵士 / 古典
        new("Velvet Soul", "r&b,soul", 74),
        new("The Midnight Groove", "r&b,funk", 67),
        new("Blue Note Collective", "jazz", 71),
        new("Swing Dynasty", "jazz,blues", 58),
        new("Opus Ensemble", "classical", 66),
        new("Orchestral Dawn", "classical,ambient", 55),
        // 民谣/乡村/独立
        new("Wildflower Creek", "folk,country", 63),
        new("The Wanderers", "folk,indie", 59),
        new("Dusty Trails", "country,folk", 70),
        new("Echo Chamber", "indie,rock", 72),
        new("Velvet Underground 2.0", "indie,folk", 57),
        // 氛围/布鲁斯/朋克
        new("Ethereal Drift", "ambient,electronic", 54),
        new("Delta Crossroads", "blues,rock", 64),
        new("Punk Reactor", "punk,rock", 68),
        // 雷鬼/放克/灵魂/拉丁
        new("Island Rhythm", "reggae,latin", 63),
        new("Groove Syndicate", "funk,soul", 72),
        new("Soul Revival", "soul,r&b", 69),
        new("Fuego Latino", "latin,dance", 71),
    ];

    private record AlbumDef(string Name, string Genre, string ArtistNames, int TotalTracks, string Year);

    private static readonly AlbumDef[] AlbumDefs =
    [
        new("Starlight", "pop", "Luna Ray", 8, "2024"),
        new("Midnight Confessions", "pop", "The Velvet Notes", 7, "2023"),
        new("Breaking Point", "rock", "Crimson Tides", 7, "2024"),
        new("Dust and Glory", "rock", "Stone Pilgrims", 6, "2023"),
        new("Neon Circuits", "electronic", "Neon Pulse", 8, "2024"),
        new("Digital Dreams", "electronic", "Digital Horizon", 7, "2023"),
        new("Bass Drop Symphony", "edm", "Circuit Waves,Bass Reactor", 8, "2024"),
        new("Deep House Sessions", "house", "Deep Sequence", 7, "2024"),
        new("Industrial Frequencies", "techno", "Void Frequency", 6, "2023"),
        new("Cosmic Grooves", "dance", "Cosmic Dance", 7, "2024"),
        new("Forged in Fire", "metal", "Iron Requiem,Black Horizon", 8, "2024"),
        new("Street Chronicles", "hip hop", "Urban Flow,MC Skyline", 8, "2023"),
        new("Flow State", "rap", "Lyric Blaze", 7, "2024"),
        new("Satin Nights", "r&b", "Velvet Soul,The Midnight Groove", 8, "2024"),
        new("Midnight Blue", "jazz", "Blue Note Collective,Swing Dynasty", 7, "2023"),
        new("Symphonic Visions", "classical", "Opus Ensemble,Orchestral Dawn", 8, "2024"),
        new("Wildflower Road", "folk", "Wildflower Creek,The Wanderers", 7, "2023"),
        new("Backroads and Bonfires", "country", "Dusty Trails", 7, "2024"),
        new("Echoes in Static", "indie", "Echo Chamber,Velvet Underground 2.0", 7, "2024"),
        new("Serenity", "ambient", "Ethereal Drift,Digital Horizon", 6, "2023"),
        new("Crossroads Blues", "blues", "Delta Crossroads,Stone Pilgrims", 7, "2024"),
        new("Anarchy in the Code", "punk", "Punk Reactor,Crimson Tides", 8, "2024"),
        new("Island Breeze", "reggae", "Island Rhythm", 7, "2023"),
        new("Funky Revolution", "funk", "Groove Syndicate,Soul Revival", 7, "2024"),
        new("Heart and Soul", "soul", "Soul Revival,Velvet Soul", 7, "2024"),
        new("Caliente", "latin", "Fuego Latino,Island Rhythm", 8, "2024"),
    ];

    private static string[] GetTrackNames(string albumName) => albumName switch
    {
        "Starlight" => ["Dancing in the Moonlight", "Electric Dreams", "Neon Nights",
            "Starlight", "Crystal Clear", "Summer Rain", "Golden Hour", "Midnight Run"],
        "Midnight Confessions" => ["Whispers", "Shadow Play", "Broken Glass",
            "Midnight Confessions", "Paper Hearts", "Fading Light", "Last Dance"],
        "Breaking Point" => ["Thunder Road", "Burning Skies", "Iron Will", "Breaking Point",
            "Raging Storm", "Into the Abyss", "Phoenix Rising"],
        "Dust and Glory" => ["Highway 61 Revisited", "Dust and Glory", "Rusty Strings",
            "The Long Road Home", "Sunset Boulevard", "Rebel Heart"],
        "Neon Circuits" => ["Binary Sunset", "Neon Circuits", "Pulse Width", "Digital Rain",
            "Cyber Dreams", "Voltage", "Feedback Loop", "Synthetic Dawn"],
        "Digital Dreams" => ["Ethereal Waves", "Cloud Surfing", "Deep Blue", "Digital Dreams",
            "Wavelength", "Subspace", "Quantum Leap"],
        "Bass Drop Symphony" => ["Drop Zone", "Frequency Shift", "Bass Cannon", "Main Stage",
            "Euphoria", "Peak Time", "The Drop", "Aftermath"],
        "Deep House Sessions" => ["Sunset Sessions", "Deep Groove", "Loft Party",
            "After Hours", "Poolside", "Night Drive", "Sunrise"],
        "Industrial Frequencies" => ["Dark Room", "Assembly Line", "Steel Pulse",
            "Machine Learning", "Zero Hour", "Heavy Industry"],
        "Cosmic Grooves" => ["Stardust", "Funky Planet", "Galactic Boogie",
            "Disco Inferno", "Moonwalker", "Space Funk", "Cosmic Grooves"],
        "Forged in Fire" => ["Molten Core", "War Machine", "Forged in Fire", "Abyssal Depths",
            "Crimson Sky", "Hammer Strike", "Unbroken", "Final Stand"],
        "Street Chronicles" => ["City Lights", "Block Party", "Concrete Jungle",
            "Street Chronicles", "Midnight Hustle", "Elevated", "Real Talk", "Day Ones"],
        "Flow State" => ["Bars of Gold", "Flow State", "Mic Check", "Underground Kings",
            "Lyrical Assassin", "Beat Switch", "The Cypher"],
        "Satin Nights" => ["Satin Nights", "Slow Burn", "Butterfly Effect", "Velvet Touch",
            "Moonlight Serenade", "Tender Moments", "Afterglow", "Love Letters"],
        "Midnight Blue" => ["Autumn Leaves", "Blue Note Serenade", "Smoke and Mirrors",
            "Midnight Blue", "Bebop Alley", "Swing Time", "Improvisation No. 3"],
        "Symphonic Visions" => ["Symphony in G Minor", "Nocturne No. 3", "Adagio for Strings",
            "Concerto for Piano", "Moonlight Sonata Reimagined", "Baroque Dreams",
            "The Four Seasons Redux", "Overture"],
        "Wildflower Road" => ["Wildflower Road", "Campfire Stories", "Mountain Air",
            "Simple Life", "River Song", "Golden Fields", "Homeward Bound"],
        "Backroads and Bonfires" => ["Backroads", "Bonfire Nights", "Pickup Truck",
            "Cold Beer", "Dirt Road Anthem", "Country Soul", "Front Porch"],
        "Echoes in Static" => ["Echoes in Static", "Lo-Fi Morning", "Tape Deck",
            "Bedroom Pop", "Static Noise", "Dream Pop", "Shoegaze"],
        "Serenity" => ["Serenity", "Weightless", "Drifting Clouds", "Zen Garden",
            "Inner Peace", "Ocean of Calm"],
        "Crossroads Blues" => ["Crossroads", "Delta Moon", "Slow Train Coming",
            "Muddy Waters Flow", "Slide Guitar Blues", "Midnight Special", "Junction"],
        "Anarchy in the Code" => ["Anarchy in the Code", "System Crash", "No Future",
            "Rebel Yell", "Break the Chains", "Punk Rock Anthem", "DIY Revolution", "Fast Lane"],
        "Island Breeze" => ["Island Breeze", "One Love", "Sunshine Vibes", "Jah Bless",
            "Reggae Sunset", "Ocean Wave", "Positive Vibration"],
        "Funky Revolution" => ["Funky Revolution", "Get Down", "Groove Is in the Heart",
            "Slap Bass Anthem", "Disco Funk", "Boogie Wonderland", "Soul Train"],
        "Heart and Soul" => ["Heart and Soul", "Soul Fire", "Rise Up", "Gospel Truth",
            "Sweet Melody", "Inner Light", "Testify"],
        "Caliente" => ["Caliente", "Salsa Nights", "Ritmo Latino", "Bailando",
            "Fuego", "Carnival", "Tropical Heat", "Mambo King"],
        _ => ["Track 1", "Track 2", "Track 3", "Track 4", "Track 5", "Track 6"],
    };
}
