﻿#if NET40
namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    unsafe partial class UnpooledUnsafeDirectByteBuffer
    {
        public override bool HasMemoryAddress => false;

        public override ref byte GetPinnableMemoryAddress() => throw new NotSupportedException();

        protected internal override short _GetShort(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetShort(addr);
        }

        protected internal override short _GetShortLE(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetShortLE(addr);
        }

        protected internal override int _GetUnsignedMedium(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetUnsignedMedium(addr);
        }

        protected internal override int _GetUnsignedMediumLE(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetUnsignedMediumLE(addr);
        }

        protected internal override int _GetInt(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetInt(addr);
        }

        protected internal override int _GetIntLE(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetIntLE(addr);
        }

        protected internal override long _GetLong(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetLong(addr);
        }

        protected internal override long _GetLongLE(int index)
        {
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.GetLongLE(addr);
        }

        public override IByteBuffer GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.buffer[index])
            {
                UnsafeByteBufferUtil.GetBytes(this, addr, index, dst, dstIndex, length);
                return this;
            }
        }

        public override IByteBuffer GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.buffer[index])
            {
                UnsafeByteBufferUtil.GetBytes(this, addr, index, dst, dstIndex, length);
                return this;
            }
        }

        protected internal override void _SetShort(int index, int value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetShort(addr, value);
        }

        protected internal override void _SetShortLE(int index, int value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetShortLE(addr, value);
        }

        protected internal override void _SetMedium(int index, int value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetMedium(addr, value);
        }

        protected internal override void _SetMediumLE(int index, int value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetMediumLE(addr, value);
        }

        protected internal override void _SetInt(int index, int value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetInt(addr, value);
        }

        protected internal override void _SetIntLE(int index, int value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetIntLE(addr, value);
        }

        protected internal override void _SetLong(int index, long value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetLong(addr, value);
        }

        protected internal override void _SetLongLE(int index, long value)
        {
            fixed (byte* addr = &this.buffer[index])
                UnsafeByteBufferUtil.SetLongLE(addr, value);
        }

        public override IByteBuffer SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.buffer[index])
            {
                UnsafeByteBufferUtil.SetBytes(this, addr, index, src, srcIndex, length);
                return this;
            }
        }

        public override IByteBuffer SetBytes(int index, byte[] src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            if (length != 0)
            {
                fixed (byte* addr = &this.buffer[index])
                {
                    UnsafeByteBufferUtil.SetBytes(this, addr, index, src, srcIndex, length);
                    return this;
                }
            }
            return this;
        }

        public override IByteBuffer GetBytes(int index, Stream output, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.buffer[index])
            {
                UnsafeByteBufferUtil.GetBytes(this, addr, index, output, length);
                return this;
            }
        }

        public override Task<int> SetBytesAsync(int index, Stream src, int length, CancellationToken cancellationToken)
        {
            this.CheckIndex(index, length);
            int read;
            fixed (byte* addr = &this.buffer[index])
            {
                read = UnsafeByteBufferUtil.SetBytes(this, addr, index, src, length);

                // See https://github.com/Azure/DotNetty/issues/436
                return TaskEx.FromResult(read);
            }
        }

        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.buffer[index])
                return UnsafeByteBufferUtil.Copy(this, addr, index, length);
        }

        public override IByteBuffer SetZero(int index, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.buffer[index])
            {
                UnsafeByteBufferUtil.SetZero(addr, length);
                return this;
            }
        }

        public override IByteBuffer WriteZero(int length)
        {
            if (length == 0) { return this; }

            this.EnsureWritable(length);
            int wIndex = this.WriterIndex;
            this.CheckIndex0(wIndex, length);
            fixed (byte* addr = &this.buffer[wIndex])
                UnsafeByteBufferUtil.SetZero(addr, length);
            this.SetWriterIndex(wIndex + length);

            return this;
        }
    }
}
#endif
