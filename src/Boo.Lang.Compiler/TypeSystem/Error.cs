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
	public abstract class AbstractType : IType, INamespace
	{	
		public abstract string Name
		{
			get;
		}
		
		public abstract EntityType EntityType
		{
			get;
		}
		
		public virtual string FullName
		{
			get
			{
				return Name;
			}
		}
		
		public virtual IType Type
		{
			get
			{
				return this;
			}
		}
		
		public virtual bool IsByRef
		{
			get
			{
				return false;
			}
		}

		public bool IsGenericTypeDefinition
		{
			get
			{
				return false;
			}
		}

		public virtual bool IsClass
		{
			get
			{
				return false;
			}
		}
		
		public virtual bool IsAbstract
		{
			get
			{
				return false;
			}
		}
		
		public virtual bool IsInterface
		{
			get
			{
				return false;
			}
		}
		
		public virtual bool IsFinal
		{
			get
			{
				return true;
			}
		}
		
		public virtual bool IsEnum
		{
			get
			{
				return false;
			}
		}
		
		public virtual bool IsValueType
		{
			get
			{
				return false;
			}
		}
		
		public virtual bool IsArray
		{
			get
			{
				return false;
			}
		}
		
		public virtual IType BaseType
		{
			get
			{
				return null;
			}
		}
		
		public virtual IType GetElementType()
		{
			return null;
		}
		
		public virtual IEntity GetDefaultMember()
		{
			return null;
		}
		
		public virtual int GetTypeDepth()
		{
			return 0;
		}
		
		public virtual bool IsSubclassOf(IType other)
		{
			return false;
		}
		
		public virtual bool IsAssignableFrom(IType other)
		{
			return false;
		}
		
		public virtual IConstructor[] GetConstructors()
		{
			return new IConstructor[0];
		}
		
		public virtual IType[] GetInterfaces()
		{
			return new IType[0];
		}
		
		public virtual IEntity[] GetMembers()
		{
			return new IEntity[0];
		}
		
		public virtual INamespace ParentNamespace
		{
			get
			{
				return null;
			}
		}
		
		public virtual bool Resolve(Boo.Lang.List targetList, string name, EntityType flags)
		{
			return false;
		}
		
		override public string ToString()
		{
			return Name;
		}

		public virtual IType BindGenericParameters(IType[] parameters)
		{
			throw new System.NotSupportedException("BindGenericParameters");
		}
	}
	
	public class Null : AbstractType
	{
		public static Null Default = new Null();
		
		private Null()
		{
		}
		
		override public string Name
		{
			get
			{
				return "null";
			}
		}
		
		override public EntityType EntityType
		{
			get
			{
				return EntityType.Null;
			}
		}
	}
	
	public class Unknown : AbstractType
	{
		public static Unknown Default = new Unknown();
		
		private Unknown()
		{
		}
		
		override public string Name
		{
			get
			{
				return "unknown";
			}
		}
		
		override public EntityType EntityType
		{
			get
			{
				return EntityType.Unknown;
			}
		}
	}
	
	public class Error : AbstractType
	{
		public static Error Default = new Error();
		
		private Error()
		{			
		}	
		
		override public string Name
		{
			get
			{
				return "error";
			}
		}
		
		override public EntityType EntityType
		{
			get
			{
				return EntityType.Error;
			}
		}
	}
}
