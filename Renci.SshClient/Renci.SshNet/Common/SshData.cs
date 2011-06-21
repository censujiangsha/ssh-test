﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Globalization;

namespace Renci.SshNet.Common
{
    /// <summary>
    /// Base ssh data serialization type
    /// </summary>
    public abstract class SshData
    {
        /// <summary>
        /// Data byte array that hold message unencrypted data
        /// </summary>
        private List<byte> _data;

        private int _readerIndex;

        /// <summary>
        /// Gets a value indicating whether all data from the buffer has been read.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is end of data; otherwise, <c>false</c>.
        /// </value>
        public bool IsEndOfData
        {
            get
            {
                return this._readerIndex >= this._data.Count();
            }
        }

        private IEnumerable<byte> _loadedData;

        /// <summary>
        /// Gets the index that represents zero in current data type.
        /// </summary>
        /// <value>
        /// The index of the zero reader.
        /// </value>
        protected virtual int ZeroReaderIndex
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets data bytes array
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetBytes()
        {
            this._data = new List<byte>();

            this.SaveData();

            return this._data.ToArray();
        }

        internal T OfType<T>() where T : SshData, new()
        {
            var result = new T();
            result.LoadBytes(this._loadedData);
            result.LoadData();
            return result;
        }

        /// <summary>
        /// Loads data from specified bytes.
        /// </summary>
        /// <param name="value">Bytes array.</param>
        public void Load(byte[] value)
        {
            this.LoadBytes(value);
            this.LoadData();
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected abstract void LoadData();

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected abstract void SaveData();

        /// <summary>
        /// Loads data bytes into internal buffer.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        protected void LoadBytes(IEnumerable<byte> bytes)
        {
            this.ResetReader();
            this._loadedData = bytes;
            this._data = new List<byte>(bytes);
        }

        /// <summary>
        /// Resets internal data reader index.
        /// </summary>
        protected void ResetReader()
        {
            this._readerIndex = this.ZeroReaderIndex;  //  Set to 1 to skip first byte which specifies message type
        }

        /// <summary>
        /// Reads all data left in internal buffer at current position.
        /// </summary>
        /// <returns></returns>
        protected byte[] ReadBytes()
        {
            var data = new byte[this._data.Count - this._readerIndex];
            this._data.CopyTo(this._readerIndex, data, 0, data.Length);
            return data;
        }

        /// <summary>
        /// Reads next specified number of bytes data type from internal buffer.
        /// </summary>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns></returns>
        protected byte[] ReadBytes(int length)
        {
            var result = new byte[length];
            this._data.CopyTo(this._readerIndex, result, 0, length);
            this._readerIndex += length;
            return result;
        }

        /// <summary>
        /// Reads next byte data type from internal buffer.
        /// </summary>
        /// <returns>Byte read.</returns>
        protected byte ReadByte()
        {
            return this.ReadBytes(1).FirstOrDefault();
        }

        /// <summary>
        /// Reads next boolean data type from internal buffer.
        /// </summary>
        /// <returns>Boolean read.</returns>
        protected bool ReadBoolean()
        {
            return this.ReadByte() == 0 ? false : true;
        }

        /// <summary>
        /// Reads next uint16 data type from internal buffer.
        /// </summary>
        /// <returns>uint16 read</returns>
        protected UInt16 ReadUInt16()
        {
            var data = this.ReadBytes(2);
            return (ushort)(data[0] << 8 | data[1]);
        }

        /// <summary>
        /// Reads next uint32 data type from internal buffer.
        /// </summary>
        /// <returns>uint32 read</returns>
        protected UInt32 ReadUInt32()
        {
            var data = this.ReadBytes(4);
            return (uint)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
        }

        /// <summary>
        /// Reads next uint64 data type from internal buffer.
        /// </summary>
        /// <returns>uint64 read</returns>
        protected UInt64 ReadUInt64()
        {
            var data = this.ReadBytes(8);
            return (uint)(data[0] << 56 | data[1] << 48 | data[2] << 40 | data[3] << 32 | data[4] << 24 | data[5] << 16 | data[6] << 8 | data[7]);
        }

        /// <summary>
        /// Reads next int64 data type from internal buffer.
        /// </summary>
        /// <returns>int64 read</returns>
        protected Int64 ReadInt64()
        {
            var data = this.ReadBytes(8);
            return (int)(data[0] << 56 | data[1] << 48 | data[2] << 40 | data[3] << 32 | data[4] << 24 | data[5] << 16 | data[6] << 8 | data[7]);
        }

        /// <summary>
        /// Reads next string data type from internal buffer.
        /// </summary>
        /// <returns>string read</returns>
        protected string ReadString()
        {
            var length = (int)this.ReadUInt32();

            if (length > int.MaxValue)
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "String that longer that {0} are not supported.", int.MaxValue));
            }

            return Encoding.ASCII.GetString(this.ReadBytes(length));
        }

        /// <summary>
        /// Reads next string data type from internal buffer.
        /// </summary>
        /// <returns>string read</returns>
        protected byte[] ReadBinaryString()
        {
            var length = (int)this.ReadUInt32();

            if (length > int.MaxValue)
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "String that longer that {0} are not supported.", int.MaxValue));
            }

            return this.ReadBytes(length);
        }
        
        /// <summary>
        /// Reads next mpint data type from internal buffer.
        /// </summary>
        /// <returns>mpint read.</returns>
        protected BigInteger ReadBigInteger()
        {
            var length = this.ReadUInt32();

            var data = this.ReadBytes((int)length);

            return new BigInteger(data.Reverse().ToArray());
        }

        /// <summary>
        /// Reads next name-list data type from internal buffer.
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<string> ReadNamesList()
        {
            var namesList = this.ReadString();
            return namesList.Split(',');
        }

        /// <summary>
        /// Reads next extension-pair data type from internal buffer.
        /// </summary>
        /// <returns></returns>
        protected IDictionary<string, string> ReadExtensionPair()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            while (this._readerIndex < this._data.Count)
            {
                var extensionName = this.ReadString();
                var extensionData = this.ReadString();
                result.Add(extensionName, extensionData);
            }
            return result;
        }

        /// <summary>
        /// Writes bytes array data into internal buffer.
        /// </summary>
        /// <param name="data">Byte array data to write.</param>
        protected void Write(IEnumerable<byte> data)
        {
            this._data.AddRange(data);
        }

        /// <summary>
        /// Writes byte data into internal buffer.
        /// </summary>
        /// <param name="data">Byte data to write.</param>
        protected void Write(byte data)
        {
            this._data.Add(data);
        }

        /// <summary>
        /// Writes boolean data into internal buffer.
        /// </summary>
        /// <param name="data">Boolean data to write.</param>
        protected void Write(bool data)
        {
            if (data)
            {
                this.Write(1);
            }
            else
            {
                this.Write(0);
            }
        }

        /// <summary>
        /// Writes uint16 data into internal buffer.
        /// </summary>
        /// <param name="data">uint16 data to write.</param>
        protected void Write(UInt16 data)
        {
            this.Write(data.GetBytes());
        }

        /// <summary>
        /// Writes uint32 data into internal buffer.
        /// </summary>
        /// <param name="data">uint32 data to write.</param>
        protected void Write(UInt32 data)
        {
            this.Write(data.GetBytes());
        }

        /// <summary>
        /// Writes uint64 data into internal buffer.
        /// </summary>
        /// <param name="data">uint64 data to write.</param>
        protected void Write(UInt64 data)
        {
            this.Write(data.GetBytes());
        }

        /// <summary>
        /// Writes int64 data into internal buffer.
        /// </summary>
        /// <param name="data">int64 data to write.</param>
        protected void Write(Int64 data)
        {
            this.Write(data.GetBytes());
        }

        /// <summary>
        /// Writes string data into internal buffer.
        /// </summary>
        /// <param name="data">string data to write.</param>
        /// <param name="encoding">String text encoding to use.</param>
        protected void Write(string data, Encoding encoding)
        {
            this.Write((uint)data.Length);
            this.Write(encoding.GetBytes(data));
        }

        /// <summary>
        /// Writes string data into internal buffer.
        /// </summary>
        /// <param name="data">string data to write.</param>
        protected void Write(string data)
        {
            this.Write(data, Encoding.ASCII);
        }

        /// <summary>
        /// Writes string data into internal buffer.
        /// </summary>
        /// <param name="data">string data to write.</param>
        protected void WriteBinaryString(byte[] data)
        {
            this.Write((uint)data.Length);
            this._data.AddRange(data);
        }

        /// <summary>
        /// Writes mpint data into internal buffer.
        /// </summary>
        /// <param name="data">mpint data to write.</param>
        protected void Write(BigInteger data)
        {
            var bytes = data.ToByteArray().Reverse().ToList();
            this.Write((uint)bytes.Count);
            this.Write(bytes);
        }

        /// <summary>
        /// Writes name-list data into internal buffer.
        /// </summary>
        /// <param name="data">name-list data to write.</param>
        protected void Write(IEnumerable<string> data)
        {
            this.Write(string.Join(",", data));
        }

        /// <summary>
        /// Writes extension-pair data into internal buffer.
        /// </summary>
        /// <param name="data">extension-pair data to write.</param>
        protected void Write(IDictionary<string, string> data)
        {
            foreach (var item in data)
            {
                this.Write(item.Key);
                this.Write(item.Value);
            }
        }
    }
}