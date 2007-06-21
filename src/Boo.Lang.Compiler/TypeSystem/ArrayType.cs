#region license
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
	public class ArrayType : IArrayType
	{	
		IType _elementType;
		
		IType _array;

		int _rank;
			
		IType _enumerable;

		public ArrayType(TypeSystemServices tagManager, IType elementType) : this(tagManager, elementType, 1)
		{
		}

		public ArrayType(TypeSystemServices tagManager, IType elementType, int rank)
		{
			_array = tagManager.ArrayType;
			_elementType = elementType;
			_rank = rank;
			_enumerable = tagManager.IEnumerableGenericType;
		}

		
		public string Name
		{
			get
			{
				if (_rank > 1)
				{
					return "(" + _elementType.ToString() + ", " + _rank + ")";
				}
				return "(" + _elementType.ToString() + ")";
			}
		}
		
		public EntityType EntityType
		{
			get			
			{
				return EntityType.Array;
			}
		}
		
		public string FullName
		{
			get
			{
				return Name;
			}
		}
		
		public IType Type
		{
			get
			{
				return this;
			}
		}
		
		public bool IsFinal
		{
			get
			{
				return true;
			}
		}
		
		public bool IsByRef
		{
			get
			{
				return false;
			}
		}
		
		public bool IsClass
		{
			get
			{
				return false;
			}
		}
		
		public bool IsInterface
		{
			get
			{
				return false;
			}
		}
		
		public bool IsAbstract
		{
			get
			{
				return false;
			}
		}
		
		public bool IsEnum
		{
			get
			{
				return false;
			}
		}
		
		public bool IsValueType
		{
			get
			{
				return false;
			}
		}
		
		public bool IsArray
		{
			get
			{
				return true;
			}
		}
		
		public int GetTypeDepth()
		{
			return 2;
		}
		
		public int GetArrayRank()
		{
			return _rank;
		}		
		
		public IType GetElementType()
		{
			return _elementType;
		}
		
		public IType BaseType
		{
			get
			{
				return _array;
			}
		}
		
		public IEntity GetDefaultMember()
		{
			return null;
		}
		
		public virtual bool IsSubclassOf(IType other)
		{
			if (other.IsAssignableFrom(_array)) return true;
			
			// Arrays also implement generic IEnumerable of their element type 
			if (other.GenericTypeInfo != null && 
				other.GenericTypeInfo.GenericDefinition == _enumerable &&
				other.GenericTypeInfo.GenericArguments[0].IsAssignableFrom(_elementType))
			{
				return true;
			}
			return false;
		}
		
		public virtual bool IsAssignableFrom(IType other)
		{			
			if (other == this || other == Null.Default)
			{
				return true;
			}
			
			if (other.IsArray)
			{
				IArrayType otherArray = (IArrayType)other;

				if (otherArray.GetArrayRank() != _rank)
				{
					return false;
				}

				IType otherEntityType = otherArray.GetElementType();
				if (_elementType.IsValueType || otherEntityType.IsValueType)
				{
					return _elementType == otherEntityType;
				}
				return _elementType.IsAssignableFrom(otherEntityType);
			}
						
			return false;
		}
		
		public IConstructor[] GetConstructors()
		{
			return new IConstructor[0];
		}
		
		public IType[] GetInterfaces()
		{
			return null;
		}
		
		public IEntity[] GetMembers()
		{
			return _array.GetMembers();
		}
		
		public INamespace ParentNamespace
		{
			get
			{
				return _array.ParentNamespace;
			}
		}
		
		public bool Resolve(List targetList, string name, EntityType flags)
		{
			return _array.Resolve(targetList, name, flags);
		}
		
		override public string ToString()
		{
			return Name;
		}

		IGenericTypeDefinitionInfo IType.GenericTypeDefinitionInfo
		{
			get { return null; }
		}
		
		IGenericTypeInfo IType.GenericTypeInfo
		{
			get { return null; }
		}
	}
}
