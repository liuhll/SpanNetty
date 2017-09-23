﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
#if !NET40
  using System.Runtime.CompilerServices;
#endif

  public static class BitOps
  {
#if !NET40
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static int RightUShift(this int value, int bits) => unchecked((int)((uint)value >> bits));

#if !NET40
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static long RightUShift(this long value, int bits) => unchecked((long)((ulong)value >> bits));
  }
}