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

namespace Boo.Lang.Compiler.Steps
{
	using Boo.Lang.Compiler.Ast;
	using Boo.Lang.Compiler.TypeSystem;
	
	public class BindTypeMembers : AbstractVisitorCompilerStep
	{
		Boo.Lang.List _parameters = new Boo.Lang.List();
		
		public BindTypeMembers()
		{
		}
		
		override public void OnMethod(Method node)
		{
			if (null == node.Entity)
			{
				node.Entity = new InternalMethod(TypeSystemServices, node);
			}
			_parameters.Add(node);
		}
		
		void BindAllParameters()
		{
			foreach (INodeWithParameters node in _parameters)
			{
				TypeMember member = (TypeMember)node;
				NameResolutionService.Restore((INamespace)TypeSystemServices.GetEntity(member.DeclaringType));
				CodeBuilder.BindParameterDeclarations(member.IsStatic, node);
			}
		}
		
		override public void OnConstructor(Constructor node)
		{
			if (null == node.Entity)
			{
				node.Entity = new InternalConstructor(TypeSystemServices, node);
			}
			_parameters.Add(node);
		}
		
		override public void OnField(Field node)
		{
			if (null == node.Entity)
			{
				node.Entity = new InternalField(TypeSystemServices, node);
			}
		}
		
		override public void OnProperty(Property node)
		{
			if (null == node.Entity)
			{				
				node.Entity = new InternalProperty(TypeSystemServices, node);
			}
			_parameters.Add(node);
			
			Visit(node.Getter);
			Visit(node.Setter);
		}	
		
		override public void OnEvent(Event node)
		{
			if (null == node.Entity)
			{
				node.Entity = new InternalEvent(TypeSystemServices, node);
			}
		}
		
		override public void OnClassDefinition(ClassDefinition node)
		{
			Visit(node.Members);
		}
		
		override public void OnModule(Module node)
		{
			Visit(node.Members);
		}
		
		override public void Run()
		{			
			NameResolutionService.Reset();
			Visit(CompileUnit.Modules);
			BindAllParameters();
		}
		
		override public void Dispose()
		{
			base.Dispose();
			_parameters.Clear();
		}
	}
}
