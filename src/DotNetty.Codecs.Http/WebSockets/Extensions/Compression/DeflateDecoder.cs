﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.WebSockets.Extensions.Compression
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Compression;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Embedded;

    /// <summary>
    /// Deflate implementation of a payload decompressor for
    /// <tt>io.netty.handler.codec.http.websocketx.WebSocketFrame</tt>.
    /// </summary>
    abstract class DeflateDecoder : WebSocketExtensionDecoder
    {
        internal static readonly IByteBuffer FrameTail = Unpooled.UnreleasableBuffer(
                Unpooled.WrappedBuffer(new byte[] { 0x00, 0x00, 0xff, 0xff }))
                .AsReadOnly();

        private readonly bool _noContext;
        private readonly IWebSocketExtensionFilter _extensionDecoderFilter;

        private EmbeddedChannel _decoder;

        /// <summary>Constructor</summary>
        /// <param name="noContext">true to disable context takeover.</param>
        /// <param name="extensionDecoderFilter">extension decoder filter.</param>
        protected DeflateDecoder(bool noContext, IWebSocketExtensionFilter extensionDecoderFilter)
        {
            if (extensionDecoderFilter is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.extensionDecoderFilter); }
            _noContext = noContext;
            _extensionDecoderFilter = extensionDecoderFilter;
        }

        /// <summary>
        /// Returns the extension decoder filter.
        /// </summary>
        protected IWebSocketExtensionFilter ExtensionDecoderFilter => _extensionDecoderFilter;

        protected abstract bool AppendFrameTail(WebSocketFrame msg);

        protected abstract int NewRsv(WebSocketFrame msg);

        protected override void Decode(IChannelHandlerContext ctx, WebSocketFrame msg, List<object> output)
        {
            if (_decoder is null)
            {
                switch (msg.Opcode)
                {
                    case Opcode.Text:
                    case Opcode.Binary:
                        break;
                    default:
                        ThrowHelper.ThrowCodecException_UnexpectedInitialFrameType(msg);
                        break;
                }

                _decoder = new EmbeddedChannel(ZlibCodecFactory.NewZlibDecoder(ZlibWrapper.None));
            }

            bool readable = msg.Content.IsReadable();
            _decoder.WriteInbound(msg.Content.Retain());
            if (AppendFrameTail(msg))
            {
                _decoder.WriteInbound(FrameTail.Duplicate());
            }

            CompositeByteBuffer compositeUncompressedContent = ctx.Allocator.CompositeDirectBuffer();
            while (true)
            {
                var partUncompressedContent = _decoder.ReadInbound<IByteBuffer>();
                if (partUncompressedContent is null)
                {
                    break;
                }

                if (!partUncompressedContent.IsReadable())
                {
                    partUncompressedContent.Release();
                    continue;
                }

                compositeUncompressedContent.AddComponent(true, partUncompressedContent);
            }

            // Correctly handle empty frames
            // See https://github.com/netty/netty/issues/4348
            if (readable && compositeUncompressedContent.NumComponents <= 0)
            {
                compositeUncompressedContent.Release();
                ThrowHelper.ThrowCodecException_CannotReadUncompressedBuf();
            }

            if (msg.IsFinalFragment && _noContext)
            {
                Cleanup();
            }

            WebSocketFrame outMsg = null;
            switch (msg.Opcode)
            {
                case Opcode.Text:
                    outMsg = new TextWebSocketFrame(msg.IsFinalFragment, NewRsv(msg), compositeUncompressedContent);
                    break;
                case Opcode.Binary:
                    outMsg = new BinaryWebSocketFrame(msg.IsFinalFragment, NewRsv(msg), compositeUncompressedContent);
                    break;
                case Opcode.Cont:
                    outMsg = new ContinuationWebSocketFrame(msg.IsFinalFragment, NewRsv(msg), compositeUncompressedContent);
                    break;
                default:
                    ThrowHelper.ThrowCodecException_UnexpectedFrameType(msg);
                    break;
            }
            output.Add(outMsg);
        }

        public override void HandlerRemoved(IChannelHandlerContext ctx)
        {
            Cleanup();
            base.HandlerRemoved(ctx);
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            Cleanup();
            base.ChannelInactive(ctx);
        }

        void Cleanup()
        {
            if (_decoder is object)
            {
                // Clean-up the previous encoder if not cleaned up correctly.
                if (_decoder.Finish())
                {
                    while (true)
                    {
                        var buf = _decoder.ReadOutbound<IByteBuffer>();
                        if (buf is null)
                        {
                            break;
                        }
                        // Release the buffer
                        buf.Release();
                    }
                }
                _decoder = null;
            }
        }
    }
}
