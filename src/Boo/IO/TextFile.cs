﻿#region license
// Copyright (c) 2004, Rodrigo B. de Oliveira (rbo@acm.org)
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
using System.IO;
using System.Text;

namespace Boo.IO
{
	public class TextFile
	{		
		public static string ReadFile(string fname)
		{
			if (null == fname)
			{
				throw new ArgumentNullException("fname");
			}
			
			using (StreamReader reader=new StreamReader(fname, Encoding.Default, true))
			{
				return reader.ReadToEnd(); 
			}
		}
		
		public static void WriteFile(string fname, string contents)
		{
			WriteFile(fname, contents, System.Text.Encoding.UTF8);
		}
		
		public static void WriteFile(string fname, string contents, System.Text.Encoding encoding)
		{
			if (null == fname)
			{
				throw new ArgumentNullException("fname");
			}
			if (null == contents)
			{
				throw new ArgumentNullException("contents");
			}
			if (null == encoding)
			{
				throw new ArgumentNullException("encoding");
			}
			
			using (StreamWriter writer=new StreamWriter(fname, false, encoding))
			{
				writer.Write(contents);
			}
		}
	}
}
