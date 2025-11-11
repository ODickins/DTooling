using Microsoft.Extensions.Logging;

namespace DTooling.DnsServer;

public partial class DnsServerInstance : IDisposable
{
    private readonly ILogger<DnsServerInstance>? _logger;

    public DnsServerInstance()
    {
    }

    public DnsServerInstance(ILogger<DnsServerInstance> logger)
    {
        _logger = logger;
    }

    private CancellationTokenSource? _cancellationTokenSource = null;
    private Task? _task = null;


    public void Start()
    {
        if (_task is not null)
            throw new InvalidOperationException("Server is already started");

        _cancellationTokenSource = new CancellationTokenSource();
        _task = ExecuteAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (_task is null)
            throw new InvalidOperationException("Server is not running");

        await _cancellationTokenSource!.CancelAsync();

        await _task
            .ContinueWith(_ => { }, TaskContinuationOptions.None);

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
        _task = null;
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _task?.Wait();
        _cancellationTokenSource?.Dispose();
        _task?.Dispose();
    }
}