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
using System.Reflection;

namespace Boo.Lang.Compiler.TypeSystem
{	
    class CachedMethod
    {
        public readonly IMethod Value;

        public CachedMethod(IMethod value)
        {
            Value = value;
        }
    }

	public class ExternalProperty : IProperty
	{
		protected TypeSystemServices _typeSystemServices;
		
		private System.Reflection.PropertyInfo _property;
		
		private IParameter[] _parameters;

		private int _isDuckTyped = -1;
		
		private int _isExtension = -1;

	    private System.Reflection.MethodInfo _accessor = null;

	    private CachedMethod _getter = null;

	    private CachedMethod _setter = null;
		
		public ExternalProperty(TypeSystemServices tagManager, System.Reflection.PropertyInfo property)
		{
			_typeSystemServices = tagManager;
			_property = property;
		}
		
		public bool IsExtension
		{
			get
			{
				if (-1 == _isExtension)
				{
					_isExtension = IsStatic && MetadataUtil.IsAttributeDefined(_property,  Types.ExtensionAttribute)
						? 1
						: 0;
				}
				return 1 == _isExtension;
			}
		}
		
		public virtual IType DeclaringType
		{
			get
			{
				return _typeSystemServices.Map(_property.DeclaringType);
			}
		}
		
		public bool IsStatic
		{
			get
			{
				return GetAccessor().IsStatic;
			}
		}
		public bool IsDuckTyped
		{
			get
			{
				if (-1 == _isDuckTyped)
				{
					_isDuckTyped =
						!_property.PropertyType.IsValueType && MetadataUtil.IsAttributeDefined(_property, Types.DuckTypedAttribute)
						? 1
						: 0;
				}
				return 1 == _isDuckTyped;
			}
		}
		
		public bool IsPublic
		{
			get
			{
			    return GetAccessor().IsPublic;
			}
		}
		
		public bool IsProtected
		{
			get
			{
			    System.Reflection.MethodInfo accessor = GetAccessor();
                return accessor.IsFamily || accessor.IsFamilyOrAssembly;
			}
		}
		
		public bool IsInternal
		{
			get
			{
			    return GetAccessor().IsAssembly;
			}
		}
		
		public bool IsPrivate
		{
			get
			{
			    return GetAccessor().IsPrivate;
			}
		}
		
		public string Name
		{
			get
			{
				return _property.Name;
			}
		}
		
		public string FullName
		{
			get
			{
				return DeclaringType.FullName + "." + Name;
			}
		}
		
		public EntityType EntityType
		{
			get
			{
				return EntityType.Property;
			}
		}
		
		public virtual IType Type
		{
			get
			{
				return _typeSystemServices.Map(_property.PropertyType);
			}
		}
		
		public System.Reflection.PropertyInfo PropertyInfo
		{
			get
			{
				return _property;
			}
		}

		public bool AcceptVarArgs
		{
			get
			{
				return false;
			}
		}
		
		public virtual IParameter[] GetParameters()
		{
            if (null != _parameters) return _parameters;

            return _parameters = _typeSystemServices.Map(_property.GetIndexParameters());
		}
		
		public virtual IMethod GetGetMethod()
		{
            if (null != _getter) return _getter.Value;
		    return (_getter = new CachedMethod(FindGetMethod())).Value;
		}

	    private IMethod FindGetMethod()
	    {
	        System.Reflection.MethodInfo getter = _property.GetGetMethod(true);
            if (null == getter)
            {
                PropertyInfo baseProperty = FindBaseProperty();
                if (null == baseProperty) return null;

                getter = baseProperty.GetGetMethod(true);
                if (null == getter) return null;
            }
	        return _typeSystemServices.Map(getter);
	    }

	    private PropertyInfo FindBaseProperty()
	    {
	        return _property.DeclaringType.BaseType.GetProperty(
                                                        _property.Name,
                                                        _property.PropertyType,
                                                        GetParameterTypes(_property.GetIndexParameters()));
	    }

	    private static Type[] GetParameterTypes(ParameterInfo[] parameters)
	    {
	        Type[] types = new Type[parameters.Length];
            for (int i=0; i<parameters.Length; ++i)
            {
                types[i] = parameters[i].ParameterType;
            }
	        return types;
	    }

        public virtual IMethod GetSetMethod()
		{
            if (null != _setter) return _setter.Value;
            return (_setter = new CachedMethod(FindSetMethod())).Value;
		}

	    private IMethod FindSetMethod()
	    {
	        System.Reflection.MethodInfo setter = _property.GetSetMethod(true);
	        if (null == setter) return null;
	        return _typeSystemServices.Map(setter);
	    }

	    override public string ToString()
		{
			return _property.ToString();
		}
		
		private System.Reflection.MethodInfo GetAccessor()
		{
            if (null != _accessor) return _accessor;

            return _accessor = FindAccessor();
		}

	    private System.Reflection.MethodInfo FindAccessor()
	    {
	        System.Reflection.MethodInfo getter = _property.GetGetMethod(true);
	        if (null != getter) return getter;
	        return _property.GetSetMethod(true);
	    }
	}
}
