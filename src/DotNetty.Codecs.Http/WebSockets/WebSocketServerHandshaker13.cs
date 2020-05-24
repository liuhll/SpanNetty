﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.WebSockets
{
    using System.Text;
    using DotNetty.Common.Utilities;

    /// <summary>
    /// Performs server side opening and closing handshakes for <a href="https://netty.io/s/rfc6455">RFC 6455</a>
    /// (originally web socket specification <a href="https://netty.io/s/ws-17">draft-ietf-hybi-thewebsocketprotocol-17</a>).
    /// </summary>
    public class WebSocketServerHandshaker13 : WebSocketServerHandshaker
    {
        public const string Websocket13AcceptGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        /// <summary>Constructor specifying the destination web socket location</summary>
        /// <param name="webSocketUrl">URL for web socket communications. e.g "ws://myhost.com/mypath". Subsequent web
        /// socket frames will be sent to this URL.</param>
        /// <param name="subprotocols">CSV of supported protocols</param>
        /// <param name="allowExtensions">Allow extensions to be used in the reserved bits of the web socket frame</param>
        /// <param name="maxFramePayloadLength">Maximum allowable frame payload length. Setting this value to your application's
        /// requirement may reduce denial of service attacks using long data frames.</param>
        public WebSocketServerHandshaker13(string webSocketUrl, string subprotocols, bool allowExtensions, int maxFramePayloadLength)
            : this(webSocketUrl, subprotocols, allowExtensions, maxFramePayloadLength, false)
        {
        }

        /// <summary>Constructor specifying the destination web socket location</summary>
        /// <param name="webSocketUrl">URL for web socket communications. e.g "ws://myhost.com/mypath". Subsequent web
        /// socket frames will be sent to this URL.</param>
        /// <param name="subprotocols">CSV of supported protocols</param>
        /// <param name="allowExtensions">Allow extensions to be used in the reserved bits of the web socket frame</param>
        /// <param name="maxFramePayloadLength">Maximum allowable frame payload length. Setting this value to your application's
        /// requirement may reduce denial of service attacks using long data frames.</param>
        /// <param name="allowMaskMismatch">When set to true, frames which are not masked properly according to the standard will still be
        /// accepted.</param>
        public WebSocketServerHandshaker13(string webSocketUrl, string subprotocols, bool allowExtensions, int maxFramePayloadLength, bool allowMaskMismatch)
            : this(webSocketUrl, subprotocols, WebSocketDecoderConfig.NewBuilder()
                .AllowExtensions(allowExtensions)
                .MaxFramePayloadLength(maxFramePayloadLength)
                .AllowMaskMismatch(allowMaskMismatch)
                .Build())
        {
        }

        /// <summary>Constructor specifying the destination web socket location</summary>
        /// <param name="webSocketURL">URL for web socket communications. e.g "ws://myhost.com/mypath". Subsequent web
        /// socket frames will be sent to this URL.</param>
        /// <param name="subprotocols">CSV of supported protocols</param>
        /// <param name="decoderConfig">Frames decoder configuration.</param>
        public WebSocketServerHandshaker13(string webSocketURL, string subprotocols, WebSocketDecoderConfig decoderConfig)
            : base(WebSocketVersion.V13, webSocketURL, subprotocols, decoderConfig)
        {
        }

        /// <summary>
        /// Handle the web socket handshake for the web socket specification <a href=
        /// "http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-17">HyBi versions 13-17</a>. Versions 13-17
        /// share the same wire protocol.
        ///
        /// <para>
        /// Browser request to the server:
        /// </para>
        ///
        /// <![CDATA[
        /// GET /chat HTTP/1.1
        /// Host: server.example.com
        /// Upgrade: websocket
        /// Connection: Upgrade
        /// Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==
        /// Origin: http://example.com
        /// Sec-WebSocket-Protocol: chat, superchat
        /// Sec-WebSocket-Version: 13
        /// ]]>
        ///
        /// <para>
        /// Server response:
        /// </para>
        ///
        /// <![CDATA[
        /// HTTP/1.1 101 Switching Protocols
        /// Upgrade: websocket
        /// Connection: Upgrade
        /// Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=
        /// Sec-WebSocket-Protocol: chat
        /// ]]>
        /// </summary>
        protected override IFullHttpResponse NewHandshakeResponse(IFullHttpRequest req, HttpHeaders headers)
        {
            if (!req.Headers.TryGet(HttpHeaderNames.SecWebsocketKey, out ICharSequence key)
                || key is null)
            {
                ThrowHelper.ThrowWebSocketHandshakeException_MissingKey();
            }

            var res = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.SwitchingProtocols,
                req.Content.Allocator.Buffer(0));
            if (headers is object)
            {
                res.Headers.Add(headers);
            }

            string acceptSeed = key.ToString() + Websocket13AcceptGuid;
            byte[] sha1 = WebSocketUtil.Sha1(Encoding.ASCII.GetBytes(acceptSeed));
            string accept = WebSocketUtil.Base64String(sha1);

            if (Logger.DebugEnabled)
            {
                Logger.WebSocketVersion13ServerHandshakeKey(key, accept);
            }

            res.Headers.Add(HttpHeaderNames.Upgrade, HttpHeaderValues.Websocket);
            res.Headers.Add(HttpHeaderNames.Connection, HttpHeaderValues.Upgrade);
            res.Headers.Add(HttpHeaderNames.SecWebsocketAccept, accept);

            if (req.Headers.TryGet(HttpHeaderNames.SecWebsocketProtocol, out ICharSequence subprotocols)
                && subprotocols is object)
            {
                string selectedSubprotocol = this.SelectSubprotocol(subprotocols.ToString());
                if (selectedSubprotocol is null)
                {
                    if (Logger.DebugEnabled)
                    {
                        Logger.RequestedSubprotocolNotSupported(subprotocols);
                    }
                }
                else
                {
                    res.Headers.Add(HttpHeaderNames.SecWebsocketProtocol, selectedSubprotocol);
                }
            }
            return res;
        }

        protected internal override IWebSocketFrameDecoder NewWebsocketDecoder() => new WebSocket13FrameDecoder(DecoderConfig);

        protected internal override IWebSocketFrameEncoder NewWebSocketEncoder() => new WebSocket13FrameEncoder(false);
    }
}
