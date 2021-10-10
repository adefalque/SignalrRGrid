using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalrRGrid.Hubs
{
    public class InvestChangeTrackerHub : Hub
    {
        static Thread workerThread;
        static CancellationTokenSource cts;

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"---> Connection established {Context.ConnectionId}");

            Clients.Client(Context.ConnectionId).SendAsync("ReceiveConnectionId", Context.ConnectionId);

            cts = new CancellationTokenSource();
            workerThread = new Thread(WatchForChanges);
            workerThread.Start(
                new WatchForChangesParameters
                {
                    ConnectionId = Context.ConnectionId,
                    ClientProxy = Clients.Client(Context.ConnectionId),
                    CancellationToken = cts.Token
                });

            return base.OnConnectedAsync();
        }

        public Task StopWatcher()
        {
            if (cts != null && workerThread != null && workerThread.ThreadState == ThreadState.Running)
            {
                cts.Cancel();
                Thread.Sleep(1000);
                cts.Dispose();
            }

            return Task.CompletedTask;
        }

        private void WatchForChanges(object parametersObj)
        {
            var parameters = (WatchForChangesParameters)parametersObj;

            do
            {
                Console.WriteLine($"Sending some new data to client {parameters.ConnectionId}");

                parameters.ClientProxy.SendAsync("ReceiveChangeTrackerData");

                Thread.Sleep(1000);
            } while (!parameters.CancellationToken.IsCancellationRequested);

            Console.WriteLine("Watcher has stopped");
        }

        private class WatchForChangesParameters
        {
            public string ConnectionId { get; set; }
            public IClientProxy ClientProxy { get; set; }
            public CancellationToken CancellationToken { get; set; }
        }
    }
}