namespace NightreignRelicSimulator.Core.Exceptions;

/// <summary>
/// データベース初期化またはアクセスに失敗したときにスローされます。
/// </summary>
public sealed class DatabaseException : Exception
{
    /// <summary>
    /// 指定したメッセージを使用して <see cref="DatabaseException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">エラーメッセージ。</param>
    public DatabaseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 指定したメッセージおよび内部例外を使用して <see cref="DatabaseException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">エラーメッセージ。</param>
    /// <param name="innerException">内部例外。</param>
    public DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
