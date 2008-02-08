﻿#region license
// Copyright (c) 2003, 2004, 2005 Rodrigo B. de Oliveira (rbo@acm.org)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of Rodrigo B. de Oliveira nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion


using System;
using System.Globalization;
using Boo.Lang.Compiler.Ast;

namespace Boo.Lang.Parser
{
	public class PrimitiveParser
	{
		public static TimeSpan ParseTimeSpan(string text)
		{
			if (text.EndsWith("ms"))
			{
				return TimeSpan.FromMilliseconds(
					ParseDouble(text.Substring(0, text.Length - 2)));
			}

			char last = text[text.Length - 1];
			double value = ParseDouble(text.Substring(0, text.Length - 1));
			switch (last)
			{
				case 's': return TimeSpan.FromSeconds(value);
				case 'h': return TimeSpan.FromHours(value);
				case 'm': return TimeSpan.FromMinutes(value);
				case 'd': return TimeSpan.FromDays(value);
			}
			throw new ArgumentException(text, "text");
		}

		public static double ParseDouble(string s)
		{
			return ParseDouble(s, false);
		}

		public static double ParseDouble(string s, bool isSingle)
		{
			double val;
			if (isSingle)
			{
				val = float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
			}
			else
			{
				val = double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
			}
			return val;
		}

		public static IntegerLiteralExpression ParseIntegerLiteralExpression(antlr.IToken token, string s, bool asLong)
		{
			const string HEX_PREFIX = "0x";
			
			NumberStyles style = NumberStyles.Integer | NumberStyles.AllowExponent;
			int hex_start = s.IndexOf(HEX_PREFIX);
			bool negative = false;

			if (hex_start >= 0)
			{
				if (s.StartsWith("-"))
				{
					negative = true;
				}
				s = s.Substring(hex_start + HEX_PREFIX.Length);
				style = NumberStyles.HexNumber;
			}

			long value = long.Parse(s, style, CultureInfo.InvariantCulture);
			if (negative) //negative hex number
			{
				value *= -1;
			}
			return new IntegerLiteralExpression(SourceLocationFactory.ToLexicalInfo(token), value, asLong || (value > int.MaxValue || value < int.MinValue));
		}

		public static int ParseInt(antlr.IToken token)
		{
			return (int) ParseIntegerLiteralExpression(token, token.getText(), false).Value;
		}

	}
}
