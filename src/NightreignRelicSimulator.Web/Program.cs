using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Data.Sqlite;
using NightreignRelicSimulator.Services.Builds;
using NightreignRelicSimulator.Services.Calculation;
using NightreignRelicSimulator.Services.Effects;
using NightreignRelicSimulator.Services.Relics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEffectService, EffectService>();
builder.Services.AddSingleton<IRelicService, RelicService>();
builder.Services.AddSingleton<IBuildService, BuildService>();
builder.Services.AddSingleton<DamageCalculator>();

var app = builder.Build();

try
{
    DatabaseInitializer.Initialize();
}
catch (DatabaseException ex)
{
    app.Logger.LogError(ex, "SQLite 初期化に失敗しました");
    throw;
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ServiceException ex)
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/effects", async (IEffectService effects, string? q, string? category, bool? forRelic, CancellationToken ct) =>
{
    IReadOnlyList<Effect> list;
    if (!string.IsNullOrWhiteSpace(q))
    {
        list = await effects.SearchByNameAsync(q, ct);
    }
    else if (!string.IsNullOrWhiteSpace(category))
    {
        list = await effects.GetByCategoryAsync(category, ct);
    }
    else
    {
        list = await effects.GetAllAsync(ct);
    }

    if (forRelic == true)
    {
        list = StagedEffectResolver.CollapseForRelicSelection(list);
    }

    return Results.Ok(list);
});

api.MapGet("/effects/staged", async (IEffectService effects, CancellationToken ct) =>
{
    var catalog = await effects.GetAllAsync(ct);
    return Results.Ok(StagedEffectResolver.GetDefinitions(catalog));
});

api.MapGet("/effects/{id:int}", async (int id, IEffectService effects, CancellationToken ct) =>
{
    var item = await effects.GetByIdAsync(id, ct);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

api.MapPost("/effects", async (Effect body, IEffectService effects, CancellationToken ct) =>
{
    var id = await effects.CreateAsync(body, ct);
    var created = await effects.GetByIdAsync(id, ct);
    return Results.Created($"/api/effects/{id}", created);
});

api.MapPut("/effects/{id:int}", async (int id, Effect body, IEffectService effects, CancellationToken ct) =>
{
    body.Id = id;
    await effects.UpdateAsync(body, ct);
    return Results.NoContent();
});

api.MapDelete("/effects/{id:int}", async (int id, IEffectService effects, CancellationToken ct) =>
{
    await effects.DeleteAsync(id, ct);
    return Results.NoContent();
});

api.MapGet("/relics", async (IRelicService relics, string? q, int? color, CancellationToken ct) =>
{
    if (!string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(await relics.SearchByNameAsync(q, ct));
    }

    if (color is int colorValue && Enum.IsDefined(typeof(RelicColor), colorValue))
    {
        return Results.Ok(await relics.GetByColorAsync((RelicColor)colorValue, ct));
    }

    return Results.Ok(await relics.GetAllAsync(ct));
});

api.MapGet("/relics/{id:int}", async (int id, IRelicService relics, CancellationToken ct) =>
{
    var detail = await relics.GetDetailAsync(id, ct);
    return detail is null ? Results.NotFound() : Results.Ok(detail);
});

api.MapPost("/relics", async (RelicUpsertRequest body, IRelicService relics, CancellationToken ct) =>
{
    var id = await relics.RegisterAsync(body, ct);
    var detail = await relics.GetDetailAsync(id, ct);
    return Results.Created($"/api/relics/{id}", detail);
});

api.MapPut("/relics/{id:int}", async (int id, RelicUpsertRequest body, IRelicService relics, CancellationToken ct) =>
{
    var request = new RelicUpsertRequest
    {
        Id = id,
        Name = body.Name,
        Color = body.Color,
        Memo = body.Memo,
        EffectIdsBySlot = body.EffectIdsBySlot
    };
    await relics.UpdateAsync(request, ct);
    return Results.NoContent();
});

api.MapDelete("/relics/{id:int}", async (int id, IRelicService relics, CancellationToken ct) =>
{
    await relics.DeleteAsync(id, ct);
    return Results.NoContent();
});

api.MapGet("/builds", async (IBuildService builds, string? q, CancellationToken ct) =>
{
    var list = string.IsNullOrWhiteSpace(q)
        ? await builds.GetAllAsync(ct)
        : await builds.SearchByNameAsync(q, ct);
    return Results.Ok(list);
});

api.MapGet("/builds/{id:int}", async (int id, IBuildService builds, CancellationToken ct) =>
{
    var detail = await builds.LoadAsync(id, ct);
    return detail is null ? Results.NotFound() : Results.Ok(detail);
});

api.MapPost("/builds", async (BuildUpsertRequest body, IBuildService builds, CancellationToken ct) =>
{
    var id = await builds.SaveAsync(body, ct);
    var detail = await builds.LoadAsync(id, ct);
    return Results.Created($"/api/builds/{id}", detail);
});

api.MapPut("/builds/{id:int}", async (int id, BuildUpsertRequest body, IBuildService builds, CancellationToken ct) =>
{
    var request = new BuildUpsertRequest
    {
        Id = id,
        Name = body.Name,
        CharacterName = body.CharacterName,
        WeaponName = body.WeaponName,
        RelicIdsByPosition = body.RelicIdsByPosition
    };
    await builds.SaveAsync(request, ct);
    return Results.NoContent();
});

api.MapDelete("/builds/{id:int}", async (int id, IBuildService builds, CancellationToken ct) =>
{
    await builds.DeleteAsync(id, ct);
    return Results.NoContent();
});

api.MapPost("/calculate", async (
    CalculateRequest body,
    IEffectService effectService,
    IBuildService builds,
    IRelicService relics,
    DamageCalculator calculator,
    CancellationToken ct) =>
{
    var detail = await builds.LoadAsync(body.BuildId, ct);
    if (detail is null)
    {
        return Results.NotFound(new { message = "ビルドが見つかりません。" });
    }

    var effects = new List<Effect>();
    foreach (var slot in detail.Slots.OrderBy(s => s.Position))
    {
        var relicDetail = await relics.GetDetailAsync(slot.Relic.Id, ct);
        if (relicDetail is null)
        {
            continue;
        }

        foreach (var effectSlot in relicDetail.Slots.OrderBy(s => s.SlotNumber))
        {
            effects.Add(effectSlot.Effect);
        }
    }

    var catalog = await effectService.GetAllAsync(ct);
    var levelOverrides = body.LevelOverrides?
        .ToDictionary(kv => kv.Key, kv => kv.Value);

    var result = calculator.Calculate(new DamageCalculationRequest
    {
        WeaponAttack = body.WeaponAttack,
        Effects = effects,
        EffectCatalog = catalog,
        LevelOverrides = levelOverrides
    });

    var stagedInBuild = StagedEffectResolver.GetDefinitions(catalog)
        .Where(d => effects.Any(e => e.EffectId == d.EffectId))
        .Select(d =>
        {
            var selected = levelOverrides is not null && levelOverrides.TryGetValue(d.EffectId, out var lv)
                ? lv
                : effects.First(e => e.EffectId == d.EffectId).Level;
            return new
            {
                d.EffectId,
                d.Name,
                SelectedLevel = selected,
                d.Levels
            };
        });

    return Results.Ok(new
    {
        result.BaseAttack,
        result.TotalMultiplier,
        result.FinalAttack,
        result.AppliedEffects,
        result.IgnoredEffects,
        result.Logs,
        StagedControls = stagedInBuild
    });
});

app.MapFallbackToFile("index.html");
app.Run();

internal sealed class CalculateRequest
{
    public int BuildId { get; init; }
    public decimal WeaponAttack { get; init; }
    public Dictionary<int, int>? LevelOverrides { get; init; }
}
