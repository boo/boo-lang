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

namespace Boo.Lang.Compiler.TypeSystem
{
	using System;
	using System.Diagnostics;

	public class AnonymousCallableType : AbstractType, ICallableType
	{
		TypeSystemServices _typeSystemServices;
		CallableSignature _signature;
		IType _concreteType;
		
		internal AnonymousCallableType(TypeSystemServices services, CallableSignature signature)
		{
			if (null == services)
			{
				throw new ArgumentNullException("services");
			}
			if (null == signature)
			{
				throw new ArgumentNullException("signature");
			}
			_typeSystemServices = services;
			_signature = signature;
		}
		
		public IType ConcreteType
		{
			get
			{
				return _concreteType;
			}
			
			set
			{
				Debug.Assert(null != value);
				_concreteType = value;
			}
		}
		
		override public IType BaseType
		{
			get
			{
				return _typeSystemServices.MulticastDelegateType;
			}
		}
		
		override public bool IsSubclassOf(IType other)
		{			
			return BaseType.IsSubclassOf(other) || other == BaseType ||
				other == _typeSystemServices.ICallableType;				
		}
		
		override public bool IsAssignableFrom(IType other)
		{
			return _typeSystemServices.IsCallableTypeAssignableFrom(this, other);
		}
		
		public CallableSignature GetSignature()
		{
			return _signature;
		}
		
		override public string Name
		{
			get
			{				
				return _signature.ToString(); 
			}
		}
		
		override public EntityType EntityType
		{
			get
			{
				return EntityType.Type;
			}
		}
		
		override public int GetTypeDepth()
		{
			return 3;
		}
	}
}
