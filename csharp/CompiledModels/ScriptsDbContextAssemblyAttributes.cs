
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyCompiledModels;
using Scripts.Data;

#pragma warning disable 219, 612, 618
#nullable disable

[assembly: DbContextModel(typeof(ScriptsDbContext), typeof(ScriptsDbContextModel))]
