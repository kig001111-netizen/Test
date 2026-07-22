using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Data.Repositories;
using NightreignRelicSimulator.Services.Common;

namespace NightreignRelicSimulator.Services.Effects;

/// <summary>
/// 効果マスタの業務処理を実装します。
/// </summary>
public sealed class EffectService : IEffectService
{
    private readonly IEffectRepository _effectRepository;
    private readonly IRelicEffectRepository _relicEffectRepository;

    /// <summary>
    /// <see cref="EffectService"/> の新しいインスタンスを初期化します。
    /// </summary>
    public EffectService()
        : this(new EffectRepository(), new RelicEffectRepository())
    {
    }

    /// <summary>
    /// テスト用コンストラクタです。
    /// </summary>
    public EffectService(IEffectRepository effectRepository, IRelicEffectRepository relicEffectRepository)
    {
        _effectRepository = effectRepository ?? throw new ArgumentNullException(nameof(effectRepository));
        _relicEffectRepository = relicEffectRepository ?? throw new ArgumentNullException(nameof(relicEffectRepository));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Effect>> GetAllAsync(CancellationToken cancellationToken = default) =>
        SqliteSession.ExecuteAsync(
            (connection, token) => _effectRepository.GetAllAsync(connection, cancellationToken: token),
            cancellationToken);

    /// <inheritdoc />
    public Task<Effect?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        SqliteSession.ExecuteAsync(
            (connection, token) => _effectRepository.GetByIdAsync(connection, id, cancellationToken: token),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Effect>> SearchByNameAsync(string keyword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        return SqliteSession.ExecuteAsync(
            (connection, token) => _effectRepository.SearchByNameAsync(connection, keyword, cancellationToken: token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Effect>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        return SqliteSession.ExecuteAsync(
            (connection, token) => _effectRepository.GetByCategoryAsync(connection, category, cancellationToken: token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CreateAsync(Effect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ValidateEffect(effect);

        return SqliteSession.ExecuteAsync(
            async (connection, token) =>
            {
                var id = await _effectRepository.InsertAsync(connection, effect, cancellationToken: token)
                    .ConfigureAwait(false);
                effect.Id = id;
                return id;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Effect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect.Id <= 0)
        {
            throw new ServiceException("更新対象の効果 ID が不正です。");
        }

        ValidateEffect(effect);

        return SqliteSession.ExecuteAsync(
            async (connection, token) =>
            {
                var updated = await _effectRepository.UpdateAsync(connection, effect, cancellationToken: token)
                    .ConfigureAwait(false);
                if (!updated)
                {
                    throw new ServiceException($"効果 ID={effect.Id} が見つかりません。");
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ServiceException("削除対象の効果 ID が不正です。");
        }

        return SqliteSession.ExecuteAsync(
            async (connection, token) =>
            {
                var references = await _relicEffectRepository
                    .GetByEffectIdAsync(connection, id, cancellationToken: token)
                    .ConfigureAwait(false);
                if (references.Count > 0)
                {
                    throw new ServiceException(
                        $"効果 ID={id} は遺物スロットで使用中のため削除できません。参照数={references.Count}");
                }

                var deleted = await _effectRepository.DeleteAsync(connection, id, cancellationToken: token)
                    .ConfigureAwait(false);
                if (!deleted)
                {
                    throw new ServiceException($"効果 ID={id} が見つかりません。");
                }
            },
            cancellationToken);
    }

    private static void ValidateEffect(Effect effect)
    {
        if (effect.EffectId <= 0)
        {
            throw new ServiceException("EffectId は 1 以上を指定してください。");
        }

        if (string.IsNullOrWhiteSpace(effect.Name))
        {
            throw new ServiceException("効果名は必須です。");
        }

        if (effect.Value <= 0m)
        {
            throw new ServiceException("Value（倍率）は 0 より大きい値を指定してください。");
        }

        if (effect.Level < 1)
        {
            throw new ServiceException("Level は 1 以上を指定してください。");
        }

        effect.Name = effect.Name.Trim();
    }
}
