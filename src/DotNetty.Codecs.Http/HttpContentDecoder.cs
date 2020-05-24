﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Embedded;

    /// <summary>
    /// Decodes the content of the received <see cref="IHttpRequest"/> and <see cref="IHttpContent"/>.
    /// The original content is replaced with the new content decoded by the
    /// <see cref="EmbeddedChannel"/>, which is created by <see cref="NewContentDecoder"/>.
    /// Once decoding is finished, the value of the <tt>'Content-Encoding'</tt>
    /// header is set to the target content encoding, as returned by <see cref="GetTargetContentEncoding"/>.
    /// Also, the <tt>'Content-Length'</tt> header is updated to the length of the
    /// decoded content.  If the content encoding of the original is not supported
    /// by the decoder, <see cref="NewContentDecoder"/> should return <code>null</code>
    /// so that no decoding occurs (i.e. pass-through).
    /// <para>
    /// Please note that this is an abstract class.  You have to extend this class
    /// and implement <see cref="NewContentDecoder"/> properly to make this class
    /// functional.  For example, refer to the source code of <see cref="HttpContentDecompressor"/>.
    /// </para>
    /// This handler must be placed after <see cref="HttpObjectDecoder"/> in the pipeline
    /// so that this handler can intercept HTTP requests after <see cref="HttpObjectDecoder"/>
    /// converts <see cref="IByteBuffer"/>s into HTTP requests.
    /// </summary>
    public abstract class HttpContentDecoder : MessageToMessageDecoder<IHttpObject>
    {
        internal static readonly AsciiString Identity = HttpHeaderValues.Identity;

        protected IChannelHandlerContext HandlerContext;
        EmbeddedChannel decoder;
        bool continueResponse;
        bool _needRead = true;

        protected override void Decode(IChannelHandlerContext context, IHttpObject message, List<object> output)
        {
            try
            {
                if (message is IHttpResponse response && response.Status.Code == StatusCodes.Status100Continue)
                {
                    if (!(response is ILastHttpContent))
                    {
                        this.continueResponse = true;
                    }
                    // 100-continue response must be passed through.
                    output.Add(ReferenceCountUtil.Retain(message));
                    return;
                }

                if (this.continueResponse)
                {
                    if (message is ILastHttpContent)
                    {
                        this.continueResponse = false;
                    }
                    // 100-continue response must be passed through.
                    output.Add(ReferenceCountUtil.Retain(message));
                    return;
                }

                var httpContent = message as IHttpContent;
                if (message is IHttpMessage httpMessage)
                {
                    this.Cleanup();
                    HttpHeaders headers = httpMessage.Headers;

                    // Determine the content encoding.
                    if (headers.TryGet(HttpHeaderNames.ContentEncoding, out ICharSequence contentEncoding))
                    {
                        contentEncoding = AsciiString.Trim(contentEncoding);
                    }
                    else
                    {
                        contentEncoding = Identity;
                    }
                    this.decoder = this.NewContentDecoder(contentEncoding);

                    if (this.decoder is null)
                    {
                        if (httpContent is object)
                        {
                            httpContent.Retain();
                        }
                        output.Add(httpMessage);
                        return;
                    }

                    // Remove content-length header:
                    // the correct value can be set only after all chunks are processed/decoded.
                    // If buffering is not an issue, add HttpObjectAggregator down the chain, it will set the header.
                    // Otherwise, rely on LastHttpContent message.
                    if (headers.Contains(HttpHeaderNames.ContentLength))
                    {
                        headers.Remove(HttpHeaderNames.ContentLength);
                        headers.Set(HttpHeaderNames.TransferEncoding, HttpHeaderValues.Chunked);
                    }
                    // Either it is already chunked or EOF terminated.
                    // See https://github.com/netty/netty/issues/5892

                    // set new content encoding,
                    ICharSequence targetContentEncoding = this.GetTargetContentEncoding(contentEncoding);
                    if (HttpHeaderValues.Identity.ContentEquals(targetContentEncoding))
                    {
                        // Do NOT set the 'Content-Encoding' header if the target encoding is 'identity'
                        // as per: http://tools.ietf.org/html/rfc2616#section-14.11
                        headers.Remove(HttpHeaderNames.ContentEncoding);
                    }
                    else
                    {
                        headers.Set(HttpHeaderNames.ContentEncoding, targetContentEncoding);
                    }

                    if (httpContent is object)
                    {
                        // If message is a full request or response object (headers + data), don't copy data part into out.
                        // Output headers only; data part will be decoded below.
                        // Note: "copy" object must not be an instance of LastHttpContent class,
                        // as this would (erroneously) indicate the end of the HttpMessage to other handlers.
                        IHttpMessage copy = null;
                        switch (httpMessage)
                        {
                            case IHttpRequest req:
                                // HttpRequest or FullHttpRequest
                                copy = new DefaultHttpRequest(req.ProtocolVersion, req.Method, req.Uri);
                                break;
                            case IHttpResponse res:
                                // HttpResponse or FullHttpResponse
                                copy = new DefaultHttpResponse(res.ProtocolVersion, res.Status);
                                break;
                            default:
                                ThrowHelper.ThrowCodecException_InvalidHttpMsg(httpMessage);
                                break;
                        }
                        copy.Headers.Set(httpMessage.Headers);
                        copy.Result = httpMessage.Result;
                        output.Add(copy);
                    }
                    else
                    {
                        output.Add(httpMessage);
                    }
                }

                if (httpContent is object)
                {
                    if (this.decoder is null)
                    {
                        output.Add(httpContent.Retain());
                    }
                    else
                    {
                        this.DecodeContent(httpContent, output);
                    }
                }
            }
            finally
            {
                _needRead = 0u >= (uint)output.Count;
            }
        }

        void DecodeContent(IHttpContent c, IList<object> output)
        {
            IByteBuffer content = c.Content;

            this.Decode(content, output);

            if (c is ILastHttpContent last)
            {
                this.FinishDecode(output);

                // Generate an additional chunk if the decoder produced
                // the last product on closure,
                HttpHeaders headers = last.TrailingHeaders;
                if (headers.IsEmpty)
                {
                    output.Add(EmptyLastHttpContent.Default);
                }
                else
                {
                    output.Add(new ComposedLastHttpContent(headers, DecoderResult.Success));
                }
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            var needRead = _needRead;
            _needRead = true;

            try
            {
                context.FireChannelReadComplete();
            }
            finally
            {
                if (needRead && !context.Channel.Configuration.AutoRead)
                {
                    context.Read();
                }
            }
        }

        protected abstract EmbeddedChannel NewContentDecoder(ICharSequence contentEncoding);

        protected ICharSequence GetTargetContentEncoding(ICharSequence contentEncoding) => Identity;

        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            this.CleanupSafely(context);
            base.HandlerRemoved(context);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            this.CleanupSafely(context);
            base.ChannelInactive(context);
        }

        public override void HandlerAdded(IChannelHandlerContext context)
        {
            this.HandlerContext = context;
            base.HandlerAdded(context);
        }

        void Cleanup()
        {
            if (this.decoder is object)
            {
                this.decoder.FinishAndReleaseAll();
                this.decoder = null;
            }
        }

        void CleanupSafely(IChannelHandlerContext context)
        {
            try
            {
                this.Cleanup();
            }
            catch (Exception cause)
            {
                // If cleanup throws any error we need to propagate it through the pipeline
                // so we don't fail to propagate pipeline events.
                context.FireExceptionCaught(cause);
            }
        }

        void Decode(IByteBuffer buf, IList<object> output)
        {
            // call retain here as it will call release after its written to the channel
            this.decoder.WriteInbound(buf.Retain());
            this.FetchDecoderOutput(output);
        }

        void FinishDecode(ICollection<object> output)
        {
            if (this.decoder.Finish())
            {
                this.FetchDecoderOutput(output);
            }
            this.decoder = null;
        }

        void FetchDecoderOutput(ICollection<object> output)
        {
            while (true)
            {
                var buf = this.decoder.ReadInbound<IByteBuffer>();
                if (buf is null)
                {
                    break;
                }
                if (!buf.IsReadable())
                {
                    buf.Release();
                    continue;
                }
                output.Add(new DefaultHttpContent(buf));
            }
        }
    }
}
