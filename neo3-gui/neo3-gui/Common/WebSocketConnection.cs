using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Neo.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Neo.Common
{
    public interface IWebSocketConnection
    {
        void PushMessage(WsMessage message);
    }
    public class WebSocketConnection:IWebSocketConnection
    {
        private readonly WebSocket _socket;

        private readonly BlockingCollection<object> _pushMessagesQueue = new BlockingCollection<object>();

        private readonly ArraySegment<byte> _buffer = WebSocket.CreateServerBuffer(4 * 1024);

        public string ConnectionId { get; set; }

        public WebSocketConnection(WebSocket socket)
        {
            _socket = socket;
            ConnectionId = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        ///  send message (json format) to connection in queue
        /// </summary>
        /// <param name="message"></param>
        public void PushMessage(WsMessage message)
        {
            if (message != null && !_pushMessagesQueue.IsAddingCompleted)
            {
                try
                {
                    _pushMessagesQueue.Add(message);
                }
                catch (InvalidOperationException)
                {
                    // Queue is already marked as complete, ignore
                }
            }
        }

        /// <summary>
        /// send message queue loop
        /// </summary>
        /// <returns></returns>
        public async Task PushLoop()
        {
            try
            {
                foreach (var msg in _pushMessagesQueue.GetConsumingEnumerable())
                {
                    // Check if WebSocket is still open before sending
                    if (_socket.State != WebSocketState.Open)
                    {
                        // WebSocket is closed, stop processing messages
                        break;
                    }
                    await SendAsync(msg);
                }
            }
            catch (WebSocketException)
            {
                // WebSocket connection closed or error occurred, exit gracefully
            }
            catch (ObjectDisposedException)
            {
                // WebSocket was disposed, exit gracefully
            }
            finally
            {
                // Mark the collection as complete to stop consuming
                if (!_pushMessagesQueue.IsAddingCompleted)
                {
                    _pushMessagesQueue.CompleteAdding();
                }
            }
        }

        /// <summary>
        /// send message (json format) to client directly
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task SendAsync(object data)
        {
            // Check WebSocket state before sending
            if (_socket.State != WebSocketState.Open)
            {
                return;
            }
            await _socket.SendAsync(data);
        }

        /// <summary>
        /// close this connection
        /// </summary>
        /// <param name="closeStatus"></param>
        /// <param name="closeDescription"></param>
        /// <returns></returns>
        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string closeDescription)
        {
            // Mark queue as complete to stop accepting new messages
            if (!_pushMessagesQueue.IsAddingCompleted)
            {
                _pushMessagesQueue.CompleteAdding();
            }

            // Only close if socket is still open
            if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await _socket.CloseAsync(closeStatus, closeDescription, CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    // Socket already closed, ignore
                }
            }
        }


 

        /// <summary>
        /// receive string from client
        /// </summary>
        /// <returns></returns>
        public async Task<WebSocketStringResult> ReceiveStringAsync()
        {
            var receiveResult= await _socket.ReceiveAsync(_buffer, CancellationToken.None);
            var result=new WebSocketStringResult(receiveResult.Count,receiveResult.MessageType,receiveResult.EndOfMessage,receiveResult.CloseStatus,receiveResult.CloseStatusDescription);
            if (result.EndOfMessage)
            {
                result.Message = Encoding.UTF8.GetString(_buffer.Array, 0, result.Count);
            }
            else
            {
                throw new Exception("too long message!");
            }
            return result;
        }






    }
}
