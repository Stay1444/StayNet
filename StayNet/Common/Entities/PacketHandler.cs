using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using StayNet.Client.Entities;
using StayNet.Common.Enums;

namespace StayNet.Common.Entities
{
    internal class PacketHandler
    {
        
        private object _sender;
        public event EventHandler<PacketInfo> PacketReceived;
        private List<KeyValuePair<TaskCompletionSource<PacketInfo>, Expression<Func<PacketInfo, bool>>>> _tcsWaiting = new();
        public PacketHandler(object sender)
        {
            this._sender = sender;
        }

        public void Handle(byte[] data)
        {
            BasePacketTypes packetType = (BasePacketTypes)data[0];
            byte[] packetData = new byte[data.Length - 1];
            Array.Copy(data, 1, packetData, 0, data.Length - 1);
            Packet packet = Packet.Create(packetData);
            PacketInfo packetInfo = new PacketInfo(packet, packetType);
            PacketReceived?.Invoke(_sender, packetInfo);
            try
            {
                for (int i = 0; i < _tcsWaiting.Count; i++)
                {
                    var tcs = _tcsWaiting[i];
                    packetInfo.Packet.Reset();
                    if (tcs.Value.Compile().Invoke(packetInfo))
                    {
                        packetInfo.Packet.Reset();
                        tcs.Key.SetResult(packetInfo);
                        _tcsWaiting.RemoveAt(i);
                        i--;
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        public Task<PacketInfo> WaitForPacket(Expression<Func<PacketInfo, bool>> filter)
        {
            TaskCompletionSource<PacketInfo> tcs = new TaskCompletionSource<PacketInfo>();
            _tcsWaiting.Add(new KeyValuePair<TaskCompletionSource<PacketInfo>, Expression<Func<PacketInfo, bool>>>(tcs, filter));
            return tcs.Task;
        }

        public async Task<PacketInfo> WaitForPacket(Expression<Func<PacketInfo, bool>> filter, CancellationToken cancellationToken)
        {
            TaskCompletionSource<PacketInfo> tcs = new TaskCompletionSource<PacketInfo>(cancellationToken);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _tcsWaiting.Add(new KeyValuePair<TaskCompletionSource<PacketInfo>, Expression<Func<PacketInfo, bool>>>(tcs, filter));
                using (cancellationToken.Register(() => {
                    // this callback will be executed when token is cancelled
                    tcs.TrySetCanceled();
                })) {
                    await tcs.Task;
                }

                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                _tcsWaiting.RemoveAll(x => x.Key == tcs);
                return null;
            }
        }
        
    }
}