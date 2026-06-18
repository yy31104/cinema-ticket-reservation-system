namespace MVC.Tests.Infrastructure;

public abstract class ApiTestBase : IDisposable
{
    protected CinemaApiFactory Factory { get; } = new();

    protected HttpClient CreateClient()
    {
        return Factory.CreateApiClient();
    }

    public void Dispose()
    {
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
