using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StayNet.Common.Entities
{
    internal class Packet
    {
        private List<byte> _data;
        public byte[] Data
        {
            get
            {
                return _data.ToArray();
            }
        }

        private int _position;
        
        public Packet(byte[] data)
        {
            _data = data.ToList();
        }
        
        public Packet(byte[] data, int position)
        {
            _data = data.ToList();
            _position = position;
        }


        public Packet()
        {
            _data = new List<byte>();
            
        }
        
        public void Reset()
        {
            _position = 0;
        }
        
        public Packet Clone()
        {
            Packet clone = new Packet(_data.ToArray());
            clone._position = _position;
            return clone;
        }
        
        public int ReadInt(bool advancePosition = true)
        {
            int value = BitConverter.ToInt32(_data.ToArray(), _position);
            if (advancePosition)
                _position += 4;
            return value;
        }
        
        public uint ReadUInt(bool advancePosition = true)
        {
            uint value = BitConverter.ToUInt32(_data.ToArray(), _position);
            if (advancePosition)
                _position += 4;
            return value;
        }
        
        public short ReadShort(bool advancePosition = true)
        {
            short value = BitConverter.ToInt16(_data.ToArray(), _position);
            if (advancePosition)
                _position += 2;
            return value;
        }
        
        public ushort ReadUShort(bool advancePosition = true)
        {
            ushort value = BitConverter.ToUInt16(_data.ToArray(), _position);
            if (advancePosition)
                _position += 2;
            return value;
        }
        
        public byte ReadByte(bool advancePosition = true)
        {
            byte value = _data[_position];
            if (advancePosition)
                _position += 1;
            return value;
        }
        
        public string ReadString(bool advancePosition = true)
        {
            int length = ReadInt();
            string value = Encoding.UTF32.GetString(_data.ToArray(), _position, length);
            if (advancePosition)
                _position += length;
            return value;
        }
        
        public byte[] ReadBytes(int length, bool advancePosition = true)
        {
            byte[] value = new byte[length];
            Buffer.BlockCopy(_data.ToArray(), _position, value, 0, length);
            if (advancePosition)
                _position += length;
            return value;
        }
        
        public void WriteInt(int value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteUInt(uint value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteShort(short value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteUShort(ushort value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteByte(byte value)
        {
            _data.Add(value);
        }
        
        public void WriteString(string value)
        {
            byte[] bytes = Encoding.UTF32.GetBytes(value);
            WriteInt(bytes.Length);
            _data.AddRange(bytes);
        }
        
        public void WriteBytes(byte[] value)
        {
            _data.AddRange(value);
        }
        
        public void WriteFloat(float value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteDouble(double value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteLong(long value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteULong(ulong value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public void WriteBool(bool value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        
        public bool ReadBool()
        {
            return ReadByte() == 1;
        }
        
        public float ReadFloat()
        {
            return BitConverter.ToSingle(_data.ToArray(), _position);
        }
        
        public double ReadDouble()
        {
            return BitConverter.ToDouble(_data.ToArray(), _position);
        }
        
        public long ReadLong()
        {
            return BitConverter.ToInt64(_data.ToArray(), _position);
        }
        
        public ulong ReadULong()
        {
            return BitConverter.ToUInt64(_data.ToArray(), _position);
        }
        
        







    }
}