﻿/*
 *  Copyright (c) 2018 Alexander Nikitin, Stanislav Denisov
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace NetStack.Threading {
	[StructLayout(LayoutKind.Explicit, Size = 192, CharSet = CharSet.Ansi)]
	public sealed class ConcurrentBuffer {
		[FieldOffset(0)]
		private readonly Cell[] _buffer;
		[FieldOffset(8)]
		private readonly int _bufferMask;
		[FieldOffset(64)]
		private int _enqueuePos;
		[FieldOffset(128)]
		private int _dequeuePos;

		public int Count {
			get {
				return _enqueuePos - _dequeuePos;
			}
		}

		public ConcurrentBuffer(int bufferSize) {
			if (bufferSize < 2)
				throw new ArgumentException("bufferSize");

			if ((bufferSize & (bufferSize - 1)) != 0)
				throw new ArgumentException("bufferSize");

			_bufferMask = bufferSize - 1;
			_buffer = new Cell[bufferSize];

			for (var i = 0; i < bufferSize; i++) {
				_buffer[i] = new Cell(i, null);
			}

			_enqueuePos = 0;
			_dequeuePos = 0;
		}

		public bool TryEnqueue(object item) {
			do {
				var buffer = _buffer;
				var pos = _enqueuePos;
				var index = pos & _bufferMask;
				var cell = buffer[index];

				if (cell.Sequence == pos && Interlocked.CompareExchange(ref _enqueuePos, pos + 1, pos) == pos) {
					buffer[index].Element = item;

					#if NET_4_6 || NET_STANDARD_2_0
						Volatile.Write(ref buffer[index].Sequence, pos + 1);
					#else
						Thread.MemoryBarrier();
						buffer[index].Sequence = pos + 1;
					#endif

					return true;
				}

				if (cell.Sequence < pos)
					return false;
			}

			while (true);
		}

		public bool TryDequeue(out object result) {
			do {
				var buffer = _buffer;
				var bufferMask = _bufferMask;
				var pos = _dequeuePos;
				var index = pos & bufferMask;
				var cell = buffer[index];

				if (cell.Sequence == pos + 1 && Interlocked.CompareExchange(ref _dequeuePos, pos + 1, pos) == pos) {
					result = cell.Element;
					buffer[index].Element = null;

					#if NET_4_6 || NET_STANDARD_2_0
						Volatile.Write(ref buffer[index].Sequence, pos + bufferMask + 1);
					#else
						Thread.MemoryBarrier();
						buffer[index].Sequence = pos + bufferMask + 1;
					#endif

					return true;
				}

				if (cell.Sequence < pos + 1) {
					result = default(object);

					return false;
				}
			}

			while (true);
		}

		[StructLayout(LayoutKind.Explicit, Size = 16, CharSet = CharSet.Ansi)]
		private struct Cell {
			[FieldOffset(0)]
			public int Sequence;
			[FieldOffset(8)]
			public object Element;

			public Cell(int sequence, object element) {
				Sequence = sequence;
				Element = element;
			}
		}
	}
}
