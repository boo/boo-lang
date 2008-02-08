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
using System.Collections.Generic;
using System.Reflection;

namespace Boo.Lang.Runtime
{
	public class ExtensionRegistry
	{
		private List<MemberInfo> _extensions = new List<MemberInfo>();
		
		public void Register(Type type)
		{
			lock (this)
			{
				_extensions = AddExtensionMembers(CopyExtensions(), type);
			}
		}

		public IEnumerable<MemberInfo> Extensions
		{
			get { lock(this) { return _extensions; }  }
		}

		public void UnRegister(Type type)
		{
			lock (this)
			{
				List<MemberInfo> extensions = CopyExtensions();
				extensions.RemoveAll(delegate(MemberInfo member) { return member.DeclaringType == type; });
				_extensions = extensions;
			}
		}

		private static List<MemberInfo> AddExtensionMembers(List<MemberInfo> extensions, Type type)
		{
			foreach (MemberInfo member in type.GetMembers(BindingFlags.Static | BindingFlags.Public))
			{
				if (!Attribute.IsDefined(member, typeof(Boo.Lang.ExtensionAttribute))) continue;
				if (extensions.Contains(member)) continue;
				extensions.Add(member);
			}
			return extensions;
		}

		private List<MemberInfo> CopyExtensions()
		{
			return new List<MemberInfo>(_extensions);
		}
	}
}
