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
using Boo.Lang.Compiler.Ast.Impl;

namespace Boo.Lang.Compiler.Ast
{
	[Serializable]
	public class ClassDefinition : ClassDefinitionImpl
	{		
		public ClassDefinition()
		{
 		}
		
		public ClassDefinition(LexicalInfo lexicalInfoProvider) : base(lexicalInfoProvider)
		{
		}
		
		public bool HasInstanceConstructor
		{
			get
			{
				foreach (TypeMember member in _members)
				{
					if (NodeType.Constructor == member.NodeType &&
						!member.IsStatic)
					{
						return true;
					}
				}
				return false;
			}
		}
		
		public Constructor GetConstructor(int index)
		{
			int current = 0;
			foreach (TypeMember member in _members)
			{
				if (member.NodeType == NodeType.Constructor)
				{
					if (current == index)
					{
						return (Constructor)member;
					}
					++current;
				}
			}
			throw new ArgumentException("index");
		}
		
		override public void Accept(IAstVisitor visitor)
		{
			visitor.OnClassDefinition(this);
		}
		
		public void Merge(ClassDefinition node)
		{
			if (null == node) throw new ArgumentNullException("node");
			if (ReferenceEquals(this, node)) return;
			this.Attributes.Extend(node.Attributes);
			this.BaseTypes.Extend(node.BaseTypes);
			this.Members.Extend(node.Members);
		}
	}
}
