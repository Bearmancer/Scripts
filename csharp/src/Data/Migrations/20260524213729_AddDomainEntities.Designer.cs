using System;
using System.Text.Json;
using CSharpScripts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CSharpScripts.src.Data.Migrations
{
    [DbContext(typeof(ScriptsDbContext))]
    [Migration("20260524213729_AddDomainEntities")]
    partial class AddDomainEntities
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CSharpScripts.Data.Entities.Album", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<int>("ArtistId")
                        .HasColumnType("integer");

                    b.Property<DateOnly?>("ReleaseDate")
                        .HasColumnType("date");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ArtistId");

                    b.HasIndex("ReleaseDate")
                        .HasDatabaseName("idx_albums_release_date");

                    b.HasIndex("ArtistId", "Title")
                        .IsUnique()
                        .HasDatabaseName("idx_albums_title");

                    b.ToTable("albums", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Artist", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<JsonDocument>("Metadata")
                        .HasColumnType("jsonb");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("idx_artists_name");

                    b.ToTable("artists", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.ExecutionLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ExitCode")
                        .HasColumnType("integer");

                    b.Property<JsonDocument>("Payload")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("SessionId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("Timestamp")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.HasIndex("SessionId")
                        .HasDatabaseName("idx_execution_logs_session_id");

                    b.HasIndex("Timestamp")
                        .HasDatabaseName("idx_execution_logs_timestamp");

                    b.ToTable("execution_logs", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.FailedTask", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ErrorMessage")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TaskName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("Timestamp")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamptz")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.HasIndex("TaskName")
                        .HasDatabaseName("idx_failed_tasks_task_name");

                    b.HasIndex("Timestamp")
                        .HasDatabaseName("idx_failed_tasks_timestamp");

                    b.ToTable("failed_tasks", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.FiberyEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasColumnType("varchar(100)");

                    b.Property<string>("FiberyId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<JsonDocument>("RawData")
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.HasIndex("EntityType")
                        .HasDatabaseName("idx_fibery_entities_entity_type");

                    b.HasIndex("FiberyId", "EntityType")
                        .IsUnique()
                        .HasDatabaseName("idx_fibery_entities_fibery_id_type");

                    b.ToTable("fibery_entities", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Scrobble", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<long>("Id"));

                    b.Property<string>("Platform")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<DateTimeOffset>("ScrobbledAt")
                        .HasColumnType("timestamptz");

                    b.Property<int>("TrackId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Platform")
                        .HasDatabaseName("idx_scrobbles_platform");

                    b.HasIndex("ScrobbledAt")
                        .HasDatabaseName("idx_scrobbles_scrobbled_at");

                    b.HasIndex("TrackId");

                    b.HasIndex("TrackId", "ScrobbledAt")
                        .IsUnique()
                        .HasDatabaseName("idx_scrobbles_timestamp");

                    b.ToTable("scrobbles", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.SourceRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<JsonDocument>("RawData")
                        .HasColumnType("jsonb");

                    b.Property<string>("SourceId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("EntityType")
                        .HasDatabaseName("idx_source_records_entity_type");

                    b.HasIndex("SourceId")
                        .HasDatabaseName("idx_source_records_source_id");

                    b.HasIndex("SourceId", "EntityType")
                        .IsUnique()
                        .HasDatabaseName("idx_source_records_source_entity_type");

                    b.ToTable("source_records", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Track", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<int>("AlbumId")
                        .HasColumnType("integer");

                    b.Property<int>("ArtistId")
                        .HasColumnType("integer");

                    b.Property<int?>("DurationSeconds")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AlbumId");

                    b.HasIndex("ArtistId");

                    b.HasIndex("Title")
                        .HasDatabaseName("idx_tracks_title");

                    b.HasIndex("ArtistId", "Title")
                        .IsUnique()
                        .HasDatabaseName("idx_tracks_artist_title");

                    b.ToTable("tracks", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Video", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<JsonDocument>("Metadata")
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset?>("SyncedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateOnly?>("UploadDate")
                        .HasColumnType("date");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ChannelName")
                        .HasDatabaseName("idx_videos_channel");

                    b.HasIndex("Title")
                        .HasDatabaseName("idx_videos_title");

                    b.HasIndex("UploadDate")
                        .HasDatabaseName("idx_videos_upload_date");

                    b.HasIndex("Url")
                        .IsUnique()
                        .HasDatabaseName("idx_videos_url");

                    b.ToTable("videos", (string)null);
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Album", b =>
                {
                    b.HasOne("CSharpScripts.Data.Entities.Artist", "Artist")
                        .WithMany("Albums")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Artist");
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Scrobble", b =>
                {
                    b.HasOne("CSharpScripts.Data.Entities.Track", "Track")
                        .WithMany("Scrobbles")
                        .HasForeignKey("TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Track");
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Track", b =>
                {
                    b.HasOne("CSharpScripts.Data.Entities.Album", "Album")
                        .WithMany("Tracks")
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CSharpScripts.Data.Entities.Artist", "Artist")
                        .WithMany("Tracks")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");

                    b.Navigation("Artist");
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Album", b =>
                {
                    b.Navigation("Tracks");
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Artist", b =>
                {
                    b.Navigation("Albums");

                    b.Navigation("Tracks");
                });

            modelBuilder.Entity("CSharpScripts.Data.Entities.Track", b =>
                {
                    b.Navigation("Scrobbles");
                });
#pragma warning restore 612, 618
        }
    }
}
