
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Scripts.Data.Entities;

#pragma warning disable 219, 612, 618
#nullable disable

namespace MyCompiledModels
{
    [EntityFrameworkInternal]
    public partial class ScrobbleEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "Scripts.Data.Entities.Scrobble",
                typeof(Scrobble),
                baseEntityType,
                propertyCount: 4,
                navigationCount: 1,
                foreignKeyCount: 1,
                unnamedIndexCount: 4,
                keyCount: 1);

            var id = runtimeEntityType.AddProperty(
                "Id",
                typeof(long),
                propertyInfo: typeof(Scrobble).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(Scrobble).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                valueGenerated: ValueGenerated.OnAdd,
                afterSaveBehavior: PropertySaveBehavior.Throw,
                sentinel: 0L);
            id.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            var platform = runtimeEntityType.AddProperty(
                "Platform",
                typeof(string),
                propertyInfo: typeof(Scrobble).GetProperty("Platform", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(Scrobble).GetField("<Platform>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            platform.SetComparer(new ValueComparer<string>(
                bool (string l, string r) => string.Equals(l, r, StringComparison.Ordinal),
                int (string v) => (v == null ? 0 : StringComparer.Ordinal.GetHashCode(v)),
                string (string v) => v));
            platform.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);
            platform.AddAnnotation("Relational:ColumnType", "varchar(50)");

            var scrobbledAt = runtimeEntityType.AddProperty(
                "ScrobbledAt",
                typeof(DateTimeOffset),
                propertyInfo: typeof(Scrobble).GetProperty("ScrobbledAt", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(Scrobble).GetField("<ScrobbledAt>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                sentinel: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
            scrobbledAt.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);
            scrobbledAt.AddAnnotation("Relational:ColumnType", "timestamptz");

            var trackId = runtimeEntityType.AddProperty(
                "TrackId",
                typeof(int),
                propertyInfo: typeof(Scrobble).GetProperty("TrackId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(Scrobble).GetField("<TrackId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                sentinel: 0);
            trackId.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);
            trackId.AddAnnotation("Relational:ColumnType", "integer");

            var key = runtimeEntityType.AddKey(
                new[] { id });
            runtimeEntityType.SetPrimaryKey(key);

            var index = runtimeEntityType.AddIndex(
                new[] { platform });
            index.AddAnnotation("Relational:Name", "idx_scrobbles_platform");

            var index0 = runtimeEntityType.AddIndex(
                new[] { scrobbledAt });
            index0.AddAnnotation("Relational:Name", "idx_scrobbles_scrobbled_at");

            var index1 = runtimeEntityType.AddIndex(
                new[] { trackId });

            var index2 = runtimeEntityType.AddIndex(
                new[] { trackId, scrobbledAt },
                unique: true);
            index2.AddAnnotation("Relational:Name", "idx_scrobbles_timestamp");

            return runtimeEntityType;
        }

        public static RuntimeForeignKey CreateForeignKey1(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
        {
            var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("TrackId") },
                principalEntityType.FindKey(new[] { principalEntityType.FindProperty("Id") }),
                principalEntityType,
                deleteBehavior: DeleteBehavior.Cascade,
                required: true);

            var track = declaringEntityType.AddNavigation("Track",
                runtimeForeignKey,
                onDependent: true,
                typeof(Track),
                propertyInfo: typeof(Scrobble).GetProperty("Track", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(Scrobble).GetField("<Track>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var scrobbles = principalEntityType.AddNavigation("Scrobbles",
                runtimeForeignKey,
                onDependent: false,
                typeof(ICollection<Scrobble>),
                propertyInfo: typeof(Track).GetProperty("Scrobbles", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(Track).GetField("<Scrobbles>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            return runtimeForeignKey;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", "music");
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "scrobbles");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}
