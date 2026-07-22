namespace NightreignRelicSimulator.Core.Exceptions;

/// <summary>
/// 業務ルール違反や Service 処理の失敗時にスローされます。
/// </summary>
public sealed class ServiceException : Exception
{
    /// <summary>
    /// 指定したメッセージを使用して <see cref="ServiceException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">エラーメッセージ。</param>
    public ServiceException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 指定したメッセージおよび内部例外を使用して <see cref="ServiceException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">エラーメッセージ。</param>
    /// <param name="innerException">内部例外。</param>
    public ServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
