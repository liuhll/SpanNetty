﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Channels.Local
{
    using System;
    using System.Net;
    using DotNetty.Common.Internal;

    public class LocalAddress : EndPoint, IComparable<LocalAddress>
    {
        public static readonly LocalAddress Any = new LocalAddress("ANY"); 

        readonly string id;
        readonly string strVal;

        internal LocalAddress(IChannel channel)
        {
            var buf = StringBuilderCache.Acquire(); // new StringBuilder(16);
            buf.Append("local:E");
            buf.Append((channel.GetHashCode() & 0xFFFFFFFFL | 0x100000000L).ToString("X"));
            buf[7] = ':';

            this.strVal = StringBuilderCache.GetStringAndRelease(buf);
            this.id = this.strVal.Substring(6);
       }

        public LocalAddress(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.id);
            }
            this.id = id.Trim().ToLowerInvariant();
            this.strVal = $"local: {this.id}";
        }

        public string Id => this.id;

        public override int GetHashCode() => this.id.GetHashCode();
        
        public override string ToString() => this.strVal;

        public int CompareTo(LocalAddress other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            
            if (other is null)
                return 1;
            
            return string.Compare(this.id, other.id, StringComparison.Ordinal);
        }
    }
}
