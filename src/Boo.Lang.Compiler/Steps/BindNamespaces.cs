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
using System.Collections;
using System.Reflection;
using List=Boo.Lang.List;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.TypeSystem;

namespace Boo.Lang.Compiler.Steps
{		
	public class BindNamespaces : AbstractCompilerStep
	{			
		override public void Run()
		{
			NameResolutionService.Reset();
			
			foreach (Boo.Lang.Compiler.Ast.Module module in CompileUnit.Modules)
			{
				foreach (Import import in module.Imports)
				{
					IEntity tag = NameResolutionService.ResolveQualifiedName(import.Namespace);					
					if (null == tag)
					{
						tag = TypeSystemServices.ErrorEntity;
						Errors.Add(CompilerErrorFactory.InvalidNamespace(import));
					}
					else
					{
						if (null != import.AssemblyReference)
						{	
							NamespaceEntity nsInfo = tag as NamespaceEntity;
							if (null == nsInfo)
							{
								Errors.Add(CompilerErrorFactory.NotImplemented(import, "assembly qualified type references"));
							}
							else
							{								
								tag = new AssemblyQualifiedNamespaceEntity(GetBoundAssembly(import.AssemblyReference), nsInfo);
							}
						}
						if (null != import.Alias)
						{
							tag = new AliasedNamespace(import.Alias.Name, tag);
							import.Alias.Entity = tag;
						}
					}
					
					_context.TraceInfo("{1}: import reference '{0}' bound to {2}.", import, import.LexicalInfo, tag.Name);
					import.Entity = tag;
				}
			}			
		}
		
		Assembly GetBoundAssembly(ReferenceExpression reference)
		{
			return ((AssemblyReference)TypeSystemServices.GetEntity(reference)).Assembly;
		}
	}
}
