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
	using System;
	using System.Collections;
	using Boo.Lang.Compiler.Ast;

	public class InternalMethod : IInternalEntity, IMethod, INamespace
	{
		public static readonly ReferenceExpression[] EmptyReferenceExpressionArray = new ReferenceExpression[0];
		
		public static readonly InternalLabel[] EmptyInternalLabelArray = new InternalLabel[0];
		
		protected TypeSystemServices _typeSystemServices;
		
		protected Boo.Lang.Compiler.Ast.Method _method;
		
		protected IMethod _override;
		
		protected ICallableType _type;
		
		protected IType _declaringType;
		
		protected IParameter[] _parameters;
		
		protected ExpressionCollection _returnExpressions;
		
		protected ExpressionCollection _yieldExpressions;
		
		protected ExpressionCollection _superExpressions;
		
		protected Boo.Lang.List _labelReferences;
		
		protected Boo.Lang.List _labels;
		
		internal InternalMethod(TypeSystemServices typeSystemServices, Boo.Lang.Compiler.Ast.Method method)
		{			
			_typeSystemServices = typeSystemServices;
			_method = method;
			if (method.NodeType != NodeType.Constructor)
			{
				if (null == _method.ReturnType)
				{
					if (_method.DeclaringType.NodeType == NodeType.ClassDefinition)
					{
						_method.ReturnType = _typeSystemServices.CodeBuilder.CreateTypeReference(Unknown.Default);
					}
					else
					{
						_method.ReturnType = _typeSystemServices.CodeBuilder.CreateTypeReference(_typeSystemServices.VoidType);
					}
				}
			}
		}
		
		public IType DeclaringType
		{
			get
			{
				if (null == _declaringType)
				{
					_declaringType = (IType)TypeSystemServices.GetEntity(_method.DeclaringType);
				}
				return _declaringType;
			}
		}
		
		public bool IsStatic
		{
			get
			{
				return _method.IsStatic;
			}
		}
		
		public bool IsPublic
		{
			get
			{
				return _method.IsPublic;
			}
		}
		
		public bool IsProtected
		{
			get
			{
				return _method.IsProtected;
			}
		}
		
		public bool IsAbstract
		{
			get
			{
				return _method.IsAbstract;
			}
		}
		
		public bool IsVirtual
		{
			get
			{
				return !_method.IsFinal;
			}
		}
		
		public bool IsSpecialName
		{
			get
			{
				return false;
			}
		}
		
		public string Name
		{
			get
			{
				return _method.Name;
			}
		}
		
		public string FullName
		{
			get
			{
				return _method.DeclaringType.FullName + "." + _method.Name;
			}
		}
		
		public virtual EntityType EntityType
		{
			get
			{
				return EntityType.Method;
			}
		}
		
		public ICallableType CallableType
		{
			get
			{
				if (null == _type)
				{
					_type = _typeSystemServices.GetCallableType(this);
				}
				return _type;
			}
		}
		
		public IType Type
		{
			get
			{
				return CallableType;
			}
		}
		
		public Method Method
		{
			get
			{
				return _method;
			}
		}
		
		public Node Node
		{
			get
			{
				return _method;
			}
		}
		
		public IMethod Override
		{
			get
			{
				return _override;
			}
			
			set
			{
				_override = value;
			}
		}
		
		public IParameter[] GetParameters()
		{
			if (null == _parameters)
			{
				_parameters = _typeSystemServices.Map(_method.Parameters);				
			}
			return _parameters;
		}
		
		public virtual IType ReturnType
		{
			get
			{					
				return TypeSystemServices.GetType(_method.ReturnType);
			}
		}
		
		public INamespace ParentNamespace
		{
			get
			{
				return DeclaringType;
			}
		}
		
		public bool IsGenerator
		{
			get
			{
				return null != _yieldExpressions;
			}
		}
		
		public ExpressionCollection ReturnExpressions
		{
			get
			{
				return _returnExpressions;
			}
		}
		
		public ExpressionCollection YieldExpressions
		{
			get
			{
				return _yieldExpressions;
			}
		}
		
		public ExpressionCollection SuperExpressions
		{
			get
			{
				return _superExpressions;
			}
		}
		
		public ReferenceExpression[] LabelReferences
		{
			get
			{
				if (null == _labelReferences)
				{
					return EmptyReferenceExpressionArray;
				}
				return (ReferenceExpression[])_labelReferences.ToArray(typeof(ReferenceExpression));
			}
		}
		
		public InternalLabel[] Labels
		{
			get
			{
				if (null == _labels)
				{
					return EmptyInternalLabelArray;
				}
				return (InternalLabel[])_labels.ToArray(typeof(InternalLabel));
			}
		}
		
		public void AddYieldExpression(Expression expression)
		{
			if (null == _yieldExpressions)
			{
				_yieldExpressions = new ExpressionCollection();
			}
			_yieldExpressions.Add(expression);
		}
		
		public void AddReturnExpression(Expression expression)
		{
			if (null == _returnExpressions)
			{
				_returnExpressions = new ExpressionCollection();
			}
			_returnExpressions.Add(expression);
		}
		
		public void AddSuperExpression(SuperLiteralExpression expression)
		{
			if (null == _superExpressions)
			{
				_superExpressions = new ExpressionCollection();
			}
			_superExpressions.Add(expression);
		}
		
		public void AddLabelReference(ReferenceExpression node)
		{
			if (null == _labelReferences)
			{
				_labelReferences = new Boo.Lang.List();
			}
			_labelReferences.Add(node);
		}
		
		public void AddLabel(InternalLabel node)
		{
			if (null == node)
			{
				throw new ArgumentNullException("node");
			}
			
			if (null == _labels)
			{
				_labels = new Boo.Lang.List();
			}
			_labels.Add(node);
		}
		
		public InternalLabel ResolveLabel(string name)
		{
			if (null != _labels)
			{
				foreach (InternalLabel label in _labels)
				{
					if (name == label.Name)
					{
						return label;
					}
				}
			}
			return null;
		}
		
		public Boo.Lang.Compiler.Ast.Local ResolveLocal(string name)
		{
			foreach (Boo.Lang.Compiler.Ast.Local local in _method.Locals)
			{
				if (local.PrivateScope)
				{
					continue;
				}
				
				if (name == local.Name)
				{
					return local;
				}
			}
			return null;
		}
		
		public ParameterDeclaration ResolveParameter(string name)
		{
			foreach (ParameterDeclaration parameter in _method.Parameters)
			{
				if (name == parameter.Name)
				{
					return parameter;
				}
			}
			return null;
		}
		
		public bool Resolve(Boo.Lang.List targetList, string name, EntityType flags)
		{			
			if (NameResolutionService.IsFlagSet(flags, EntityType.Local))
			{
				Boo.Lang.Compiler.Ast.Local local = ResolveLocal(name);
				if (null != local)
				{
					targetList.Add(TypeSystemServices.GetEntity(local));
					return true;
				}
			}
			
			if (NameResolutionService.IsFlagSet(flags, EntityType.Parameter))
			{
				ParameterDeclaration parameter = ResolveParameter(name);
				if (null != parameter)
				{
					targetList.Add(TypeSystemServices.GetEntity(parameter));
					return true;
				}
			}

			return false;
		}
		
		IEntity[] INamespace.GetMembers()
		{
			return NullNamespace.EmptyEntityArray;
		}
		
		override public string ToString()
		{
			return _typeSystemServices.GetSignature(this);
		}
	}
}
