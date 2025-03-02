namespace SingleInstance.Services;

using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Hosting;

class PipeService : IHostedService
{
    private const string PIPE_NAME = "com.albertakhmetov.singleinstance.pipe";

    public static async Task SendData(string data)
    {
        using (var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out, PipeOptions.Asynchronous))
        {
            await pipeClient.ConnectAsync(1000);

            using (var writer = new StreamWriter(pipeClient, Encoding.UTF8))
            {
                await writer.WriteAsync(data);
                await writer.FlushAsync();
            }
        }
    }

    private readonly ISingleInstanceService singleInstanceService;
    private CancellationTokenSource tokenSource;

    public PipeService(ISingleInstanceService singleInstanceService)
    {
        this.singleInstanceService = singleInstanceService ?? throw new ArgumentNullException(nameof(singleInstanceService));
        tokenSource = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(async x =>
        {
            var token = (CancellationToken)x!;

            while (true)
            {
                using (var pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    await pipeServer.WaitForConnectionAsync(token);

                    using (var reader = new StreamReader(pipeServer, Encoding.UTF8))
                    {
                        var receivedData = await reader.ReadToEndAsync();

                        singleInstanceService.OnActivated(receivedData);
                    }
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }
            }
        }, tokenSource.Token);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return tokenSource.CancelAsync();
    }
}
