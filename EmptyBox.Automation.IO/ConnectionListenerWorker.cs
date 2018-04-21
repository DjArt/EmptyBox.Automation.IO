using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EmptyBox.IO.Network;

namespace EmptyBox.Automation.Network
{
    public sealed class ConnectionListenerWorker : Pipeline<IConnectionListener, IConnection>, IPipelineIO<IConnectionListener, IConnection>
    {
        event EventHandler<IConnection> IPipelineOutput<IConnection>.Output { add => Output += value; remove => Output -= value; }

        EventHandler<IConnectionListener> IPipelineInput<IConnectionListener>.Input => async (sender, output) =>
        {
            Handlers.Add(output);
            output.ConnectionSocketReceived += ConnectionSocketReceived;
            SocketOperationStatus status = await output.Start();
            switch (status)
            {
                case SocketOperationStatus.Success:
                    break;
                default:
                    Handlers.Remove(output);
                    output.ConnectionSocketReceived -= ConnectionSocketReceived;
                    break;
            }
        };

        private List<IConnectionListener> Handlers;

        private event EventHandler<IConnection> Output;
        
        public ConnectionListenerWorker()
        {
            Handlers = new List<IConnectionListener>();
        }

        private void ConnectionSocketReceived(IConnectionListener handler, IConnection socket)
        {
            Output?.Invoke(this, socket);
        }
    }
}