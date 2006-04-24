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
using System.Globalization;
using System.Threading;

namespace Boo.Lang
{
	/// <summary>
	/// Resource manager.
	/// </summary>
	public sealed class ResourceManager
	{
		static System.Resources.ResourceManager _rm = new System.Resources.ResourceManager("strings", typeof(ResourceManager).Assembly);

		private ResourceManager()
		{
		}

		public static string GetString(string name)
		{
			//return _rm.GetString(name);
			//TODO: uncomment above and comment below when mono bug 77242 fixed
			string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                        if (lang == "pt" || lang == "it")
                                return _rm.GetString(name);
			//this is so the boo br locale test will pass:
			lang = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
                        if (lang == "pt" || lang == "it")
                                return _rm.GetString(name);
                        return _rm.GetString(name, CultureInfo.InvariantCulture);
		}

		public static string Format(string name, params object[] args)
		{
			return string.Format(GetString(name), args);
		}
		
		public static string Format(string name, object param)
		{
			return string.Format(GetString(name), param);
		}
	}
}
