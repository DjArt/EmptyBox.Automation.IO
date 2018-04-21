using EmptyBox.IO.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EmptyBox.Automation.IO
{
    public class ConnectionWorker : Pipeline<(IConnection Connection, byte[] Message), (IConnection Connection, byte[] Message)>, IPipelineIO<(IConnection Connection, byte[] Message), (IConnection Connection, byte[] Message)>, IPipelineInput<IConnection>
    {
        EventHandler<IConnection> IPipelineInput<IConnection>.Input => (x, y) =>
        {
            y.ConnectionInterrupt += ConnectionInterrupt;
            y.MessageReceived += MessageReceived;
            Notify_Connected.Send(y);
        };

        EventHandler<(IConnection Connection, byte[] Message)> IPipelineInput<(IConnection Connection, byte[] Message)>.Input => async (x, y) =>
        {
            SocketOperationStatus status = await y.Connection.Send(y.Message);
            switch (status)
            {
                case SocketOperationStatus.Success:
                    break;
                default:
                    Notify_SendFailed.Send(y);
                    break;
            }
        };

        event EventHandler<(IConnection Connection, byte[] Message)> IPipelineOutput<(IConnection Connection, byte[] Message)>.Output
        {
            add => Output += value;
            remove => Output -= value;
        }

        private event EventHandler<(IConnection Connection, byte[] Message)> Output;
        
        public ExternalInput<IConnection> Notify_Connected { get; private set; }
        public ExternalInput<IConnection> Notify_Disconnected { get; private set; }
        public ExternalInput<(IConnection Connection, byte[] Message)> Notify_SendSuccess { get; private set; }
        public ExternalInput<(IConnection Connection, byte[] Message)> Notify_SendFailed { get; private set; }

        public ConnectionWorker()
        {
            Notify_Connected = new ExternalInput<IConnection>();
            Notify_Disconnected = new ExternalInput<IConnection>();
            Notify_SendFailed = new ExternalInput<(IConnection Connection, byte[] Message)>();
            Notify_SendSuccess = new ExternalInput<(IConnection Connection, byte[] Message)>();
        }

        private void MessageReceived(IConnection connection, byte[] message)
        {
            Output?.Invoke(this, (connection, message));
        }

        private void ConnectionInterrupt(IConnection connection)
        {
            connection.MessageReceived -= MessageReceived;
            connection.ConnectionInterrupt -= ConnectionInterrupt;
            Notify_Disconnected.Send(connection);
        }
    }
}