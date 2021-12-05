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
        private List<KeyValuePair<BasePacketTypes, KeyValuePair<TaskCompletionSource<PacketInfo>, Expression<Func<PacketInfo, bool>>>>> _tcsWaiting = new();
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

                    var d = _tcsWaiting[i];
                   
                    if (d.Key != packetType)
                    {

                        continue;
                    }


                    
                    var tcs = d.Value;
                    packetInfo.Packet.Reset();
                    try
                    {
                        if (tcs.Value.Compile().Invoke(packetInfo))
                        {

                        
                            if (tcs.Key.TrySetResult(packetInfo))
                            {
                                _tcsWaiting.RemoveAt(i);
                                packetInfo.Packet.Reset();
                            }

                            break;
                        }
                    }catch(Exception e)
                    {
                        tcs.Key.TrySetException(e);
                        _tcsWaiting.RemoveAt(i);
                        packetInfo.Packet.Reset();
                        break;
                    }
                }
            }catch(Exception e)
            {
                
            }
        }
        
        public Task<PacketInfo> WaitForPacket(BasePacketTypes type, Expression<Func<PacketInfo, bool>> filter)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            return WaitForPacket(type, filter, cancellationTokenSource.Token);
        }

        public async Task<PacketInfo> WaitForPacket(BasePacketTypes type, Expression<Func<PacketInfo, bool>> filter, CancellationToken cancellationToken)
        {
            TaskCompletionSource<PacketInfo> tcs = new TaskCompletionSource<PacketInfo>(cancellationToken);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _tcsWaiting.Add(new KeyValuePair<BasePacketTypes, KeyValuePair<TaskCompletionSource<PacketInfo>, Expression<Func<PacketInfo, bool>>>>(type, new KeyValuePair<TaskCompletionSource<PacketInfo>, Expression<Func<PacketInfo, bool>>>(tcs, filter)));
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
                try
                {
                    _tcsWaiting.RemoveAll(x => x.Value.Key == tcs);
                }catch(Exception e)
                {
                    
                }

                return null;
            }
        }
        
    }
}