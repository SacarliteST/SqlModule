namespace SQLModule.Domain.Common;

/// <summary>
/// Временно отключает защиту учебной базы, используемой опубликованными заданиями. Только для системных
/// операций (сидеры); пользовательские команды защиту обходить не должны.
/// </summary>
public static class TargetDbEditBypass
{
    private static readonly AsyncLocal<int> Depth = new();

    public static bool IsActive => Depth.Value > 0;

    public static IDisposable Begin()
    {
        Depth.Value++;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Depth.Value--;
        }
    }
}
