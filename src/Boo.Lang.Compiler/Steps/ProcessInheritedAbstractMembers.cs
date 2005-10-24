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

#region license
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

namespace Boo.Lang.Compiler.Steps
{
	using System.Diagnostics;
	using Boo.Lang.Compiler.Ast;
	using Boo.Lang.Compiler.TypeSystem;

	public class ProcessInheritedAbstractMembers : AbstractVisitorCompilerStep
	{
		private Boo.Lang.List _newAbstractClasses;

		public ProcessInheritedAbstractMembers()
		{
		}

		override public void Run()
		{	
			_newAbstractClasses = new List();
			Visit(CompileUnit.Modules);
			ProcessNewAbstractClasses();
		}

		override public void Dispose()
		{
			_newAbstractClasses = null;
			base.Dispose();
		}

		override public void OnProperty(Property node)
		{
			if (node.IsAbstract)
			{
				if (null == node.Type)
				{
					node.Type = CodeBuilder.CreateTypeReference(TypeSystemServices.ObjectType);
				}
			}

			Visit(node.ExplicitInfo);
		}

		override public void OnMethod(Method node)
		{
			if (node.IsAbstract)
			{
				if (null == node.ReturnType)
				{
					node.ReturnType = CodeBuilder.CreateTypeReference(TypeSystemServices.VoidType);
				}
			}
			Visit(node.ExplicitInfo);
		}

		override public void OnExplicitMemberInfo(ExplicitMemberInfo node)
		{
			TypeMember member = (TypeMember)node.ParentNode;
			CheckExplicitMemberValidity((IExplicitMember)member);
			member.Visibility = TypeMemberModifiers.Private;
		}

		void CheckExplicitMemberValidity(IExplicitMember node)
		{
			IMember explicitMember = (IMember)GetEntity((Node)node);
			if (explicitMember.DeclaringType.IsClass)
			{
				IType targetInterface = GetType(node.ExplicitInfo.InterfaceType);
				if (!targetInterface.IsInterface)
				{
					Error(CompilerErrorFactory.InvalidInterfaceForInterfaceMember((Node)node, node.ExplicitInfo.InterfaceType.Name));
				}
				
				if (!explicitMember.DeclaringType.IsSubclassOf(targetInterface))
				{
					Error(CompilerErrorFactory.InterfaceImplForInvalidInterface((Node)node, targetInterface.Name, ((TypeMember)node).Name));
				}
			}
			else
			{
				// TODO: Only class ITM's can do explicit interface methods
			}
		}

		override public void LeaveInterfaceDefinition(InterfaceDefinition node)
		{
			MarkVisited(node);
		}

		override public void LeaveClassDefinition(ClassDefinition node)
		{
			MarkVisited(node);
			foreach (TypeReference baseTypeRef in node.BaseTypes)
			{	
				IType baseType = GetType(baseTypeRef);
				EnsureRelatedNodeWasVisited(node, baseType);
				if (baseType.IsInterface)
				{
					ResolveInterfaceMembers(node, baseTypeRef, baseType);
				}
				else
				{
					if (IsAbstract(baseType))
					{
						ResolveAbstractMembers(node, baseTypeRef, baseType);
					}
				}
			}
		}

		bool IsAbstract(IType type)
		{
			if (type.IsAbstract)
			{
				return true;
			}
			
			AbstractInternalType internalType = type as AbstractInternalType;
			if (null != internalType)
			{
				return _newAbstractClasses.Contains(internalType.TypeDefinition);
			}
			return false;
		}

		IProperty GetPropertyEntity(TypeMember member)
		{
			return (IProperty)GetEntity(member);
		}
		
		void ResolveClassAbstractProperty(ClassDefinition node,
			TypeReference baseTypeRef,
			IProperty entity)
		{
			foreach (TypeMember member in node.Members)
			{
				if (entity.Name != member.Name
					|| NodeType.Property != member.NodeType
					|| !IsCorrectExplicitMemberImplOrNoExplicitMemberAtAll(member, entity)
					|| !TypeSystemServices.CheckOverrideSignature(entity.GetParameters(), GetPropertyEntity(member).GetParameters()))
				{
					continue;
				}

				Property p = (Property)member;
				ProcessPropertyAccessor(p, p.Getter, entity.GetGetMethod());
				ProcessPropertyAccessor(p, p.Setter, entity.GetSetMethod());
				if (null == p.Type)
				{
					p.Type = CodeBuilder.CreateTypeReference(entity.Type);
				}
				else
				{
					if (entity.Type != p.Type.Entity)
					{
						Error(CompilerErrorFactory.ConflictWithInheritedMember(p, p.FullName, entity.FullName));
					}
				}
				return;
			}
			
			node.Members.Add(CreateAbstractProperty(baseTypeRef, entity));
			AbstractMemberNotImplemented(node, baseTypeRef, entity);
		}

		private void ProcessPropertyAccessor(Property p, Method accessor, IMethod method)
		{
			if (null != accessor)
			{
				accessor.Modifiers |= TypeMemberModifiers.Virtual;
				if (null != p.ExplicitInfo)
				{
					accessor.ExplicitInfo = p.ExplicitInfo.CloneNode();
					accessor.ExplicitInfo.Entity = method;
					accessor.Visibility = TypeMemberModifiers.Private;
				}
			}
		}

		Property CreateAbstractProperty(TypeReference reference, IProperty property)
		{
			Debug.Assert(0 == property.GetParameters().Length);
			Property p = CodeBuilder.CreateProperty(property.Name, property.Type);
			p.Modifiers |= TypeMemberModifiers.Abstract;
			
			IMethod getter = property.GetGetMethod();
			if (getter != null)
			{
				p.Getter = CodeBuilder.CreateAbstractMethod(reference.LexicalInfo, getter);
			}
			
			IMethod setter = property.GetSetMethod();
			if (setter != null)
			{
				p.Setter = CodeBuilder.CreateAbstractMethod(reference.LexicalInfo, setter);
			}
			return p;
		}
		
		void ResolveAbstractEvent(ClassDefinition node,
			TypeReference baseTypeRef,
			IEvent entity)
		{
			TypeMember member = node.Members[entity.Name];
			if (null != member)
			{
				Event ev = (Event)member;

				Method add = ev.Add;
				if (add != null)
				{
					add.Modifiers |= TypeMemberModifiers.Final | TypeMemberModifiers.Virtual;
				}

				Method remove = ev.Remove;
				if (remove != null)
				{
					remove.Modifiers |= TypeMemberModifiers.Final | TypeMemberModifiers.Virtual;
				}

				Method raise = ev.Remove;
				if (raise != null)
				{
					raise.Modifiers |= TypeMemberModifiers.Final | TypeMemberModifiers.Virtual;
				}

				_context.TraceInfo("{0}: Event {1} implements {2}", ev.LexicalInfo, ev, entity);
				return;
			}

			node.Members.Add(CodeBuilder.CreateAbstractEvent(baseTypeRef.LexicalInfo, entity));
			AbstractMemberNotImplemented(node, baseTypeRef, entity);
		}

		void ResolveAbstractMethod(ClassDefinition node,
			TypeReference baseTypeRef,
			IMethod entity)
		{
			if (entity.IsSpecialName)
			{
				return;
			}
			
			foreach (TypeMember member in node.Members)
			{
				if (entity.Name == member.Name
					&& NodeType.Method == member.NodeType
					&& IsCorrectExplicitMemberImplOrNoExplicitMemberAtAll(member, entity))
				{
					Method method = (Method)member;
					if (TypeSystemServices.CheckOverrideSignature(GetEntity(method), entity))
					{	
						if (IsUnknown(method.ReturnType))
						{
							method.ReturnType = CodeBuilder.CreateTypeReference(entity.ReturnType);
						}
						else
						{	
							if (entity.ReturnType != method.ReturnType.Entity)
							{
								Error(CompilerErrorFactory.ConflictWithInheritedMember(method, method.FullName, entity.FullName));
							}
						}

						if (null != method.ExplicitInfo)
						{
							method.ExplicitInfo.Entity = entity;
						}

						if (!method.IsOverride && !method.IsVirtual)
						{
							method.Modifiers |= TypeMemberModifiers.Virtual;
						}
						
						_context.TraceInfo("{0}: Method {1} implements {2}", method.LexicalInfo, method, entity);
						return;
					}
				}
			}
			
			node.Members.Add(CodeBuilder.CreateAbstractMethod(baseTypeRef.LexicalInfo, entity));
			AbstractMemberNotImplemented(node, baseTypeRef, entity);
		}

		private bool IsCorrectExplicitMemberImplOrNoExplicitMemberAtAll(TypeMember member, IMember entity)
		{
			ExplicitMemberInfo info = ((IExplicitMember)member).ExplicitInfo;
			return info == null
				|| entity.DeclaringType == GetType(info.InterfaceType);
		}

		bool IsUnknown(TypeReference typeRef)
		{
			return Unknown.Default == typeRef.Entity;
		}
		
		void AbstractMemberNotImplemented(ClassDefinition node, TypeReference baseTypeRef, IMember member)
		{
			if (IsValueType(node))
			{
				Error(CompilerErrorFactory.ValueTypeCantHaveAbstractMember(baseTypeRef, node.FullName, GetAbstractMemberSignature(member)));
			}
			else if (!node.IsAbstract)
			{
				Warnings.Add(
					CompilerWarningFactory.AbstractMemberNotImplemented(baseTypeRef,
					node.FullName, GetAbstractMemberSignature(member)));
				_newAbstractClasses.AddUnique(node);
			}
		}

		private bool IsValueType(ClassDefinition node)
		{
			return ((IType)node.Entity).IsValueType;
		}

		private string GetAbstractMemberSignature(IMember member)
		{
			IMethod method = member as IMethod;
			return method != null
				? TypeSystemServices.GetSignature(method)
				: member.FullName;
		}

		void ResolveInterfaceMembers(ClassDefinition node,
			TypeReference baseTypeRef,
			IType baseType)
		{
			foreach (IType entity in baseType.GetInterfaces())
			{
				ResolveInterfaceMembers(node, baseTypeRef, entity);
			}
			
			foreach (IMember entity in baseType.GetMembers())
			{
				ResolveAbstractMember(node, baseTypeRef, entity);
			}
		}
		
		void ResolveAbstractMembers(ClassDefinition node,
			TypeReference baseTypeRef,
			IType baseType)
		{
			foreach (IEntity member in baseType.GetMembers())
			{
				switch (member.EntityType)
				{
					case EntityType.Method:
					{
						IMethod method = (IMethod)member;
						if (method.IsAbstract)
						{
							ResolveAbstractMethod(node, baseTypeRef, method);
						}
						break;
					}
					
					case EntityType.Property:
					{
						IProperty property = (IProperty)member;
						if (IsAbstractAccessor(property.GetGetMethod()) ||
							IsAbstractAccessor(property.GetSetMethod()))
						{
							ResolveClassAbstractProperty(node, baseTypeRef, property);
						}
						break;
					}

					case EntityType.Event:
					{
						IEvent ev = (IEvent)member;
						if (ev.IsAbstract)
						{
							ResolveAbstractEvent(node, baseTypeRef, ev);
						}
						break;
					}
					
				}
			}
		}
		
		bool IsAbstractAccessor(IMethod accessor)
		{
			if (null != accessor)
			{
				return accessor.IsAbstract;
			}
			return false;
		}
		
		void ResolveAbstractMember(ClassDefinition node,
			TypeReference baseTypeRef,
			IMember member)
		{
			switch (member.EntityType)
			{
				case EntityType.Method:
				{
					ResolveAbstractMethod(node, baseTypeRef, (IMethod)member);
					break;
				}
				
				case EntityType.Property:
				{
					ResolveClassAbstractProperty(node, baseTypeRef, (IProperty)member);
					break;
				}

				case EntityType.Event:
				{
					ResolveAbstractEvent(node, baseTypeRef, (IEvent)member);
					break;
				}
				
				default:
				{
					NotImplemented(baseTypeRef, "abstract member: " + member);
					break;
				}
			}
		}
		
		void ProcessNewAbstractClasses()
		{
			foreach (ClassDefinition node in _newAbstractClasses)
			{
				node.Modifiers |= TypeMemberModifiers.Abstract;
			}
		}
	}
}
