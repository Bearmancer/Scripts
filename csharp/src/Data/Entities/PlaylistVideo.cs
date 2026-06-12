using System;

namespace Scripts.Data.Entities;

public sealed class PlaylistVideo
{
    public int PlaylistId { get; set; }
    public int VideoId { get; set; }
    public int Position { get; set; }

    public Playlist Playlist { get; set; } = null!;
    public Video Video { get; set; } = null!;
}
