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

namespace Boo.Lang.Compiler.Steps
{
	using Boo.Lang.Compiler;
	using Boo.Lang.Compiler.Ast;
	using Boo.Lang.Compiler.TypeSystem;
	using System;
	using System.Reflection;
	using System.Collections;
	
	public class InitializeNameResolutionService : AbstractVisitorCompilerStep
	{
		Hashtable _namespaces = new Hashtable();
		
		public InitializeNameResolutionService()
		{
		}
		
		override public void Run()
		{				
			ResolveImportAssemblyReferences();
			
			OrganizeExternalNamespaces();
			ResolveInternalModules();
			
			NameResolutionService.GlobalNamespace = new GlobalNamespace(_namespaces);
		}
		
		void ResolveInternalModules()
		{
			foreach (Boo.Lang.Compiler.Ast.Module module in CompileUnit.Modules)
			{
				TypeSystem.ModuleEntity moduleEntity = new TypeSystem.ModuleEntity(NameResolutionService, TypeSystemServices, module);
				module.Entity = moduleEntity;
				
				NamespaceDeclaration namespaceDeclaration = module.Namespace;
				if (null != namespaceDeclaration)
				{
					module.Imports.Add(new Import(namespaceDeclaration.LexicalInfo, namespaceDeclaration.Name));
				}
				GetNamespace(moduleEntity.Namespace).AddModule(moduleEntity);
			}
		}
		
		Assembly FindOrLoadAssembly(string name)
		{
			Assembly found = Parameters.References.Find(name);
			if (null == found)
			{
				found = Assembly.LoadWithPartialName(name);
				if (null == found)
				{
					found = Assembly.Load(name);
				}
			}
			return found;
		}
		
		void ResolveImportAssemblyReferences()
		{
			foreach (Boo.Lang.Compiler.Ast.Module module in CompileUnit.Modules)
			{
				ImportCollection imports = module.Imports;
				Import[] importArray = imports.ToArray();
				for (int i=0; i<importArray.Length; ++i)
				{
					Import u = importArray[i];
					ReferenceExpression reference = u.AssemblyReference;
					if (null != reference)
					{
						try
						{
							Assembly asm = FindOrLoadAssembly(reference.Name);
							Parameters.References.Add(asm);
							reference.Entity = new TypeSystem.AssemblyReference(asm);
						}
						catch (Exception x)
						{
							Errors.Add(CompilerErrorFactory.UnableToLoadAssembly(reference, reference.Name, x));
							imports.RemoveAt(i);							
						}
					}
				}
			}
		}
		
		void OrganizeExternalNamespaces()
		{	
			foreach (Assembly asm in Parameters.References)
			{				
				try
				{
					OrganizeAssemblyTypes(asm);
				}
				catch (Exception x)
				{
					Errors.Add(
						CompilerErrorFactory.FailedToLoadTypesFromAssembly(asm.FullName, x));
				}
			}
		}
		
		void OrganizeAssemblyTypes(Assembly asm)
		{
			Type[] types = asm.GetTypes();				
			foreach (Type type in types)
			{
				if (type.IsPublic)
				{
					string ns = type.Namespace;
					if (null == ns)
					{
						ns = string.Empty;
					}					
				
					GetNamespace(ns).Add(type);
				}
			}
		}
		
		NamespaceEntity GetNamespace(string ns)
		{
			string[] namespaceHierarchy = ns.Split('.');
			string topLevelName = namespaceHierarchy[0];
			NamespaceEntity topLevel = GetTopLevelNamespace(topLevelName);
			NamespaceEntity current = topLevel;
			for (int i=1; i<namespaceHierarchy.Length; ++i)
			{
				current = current.GetChildNamespace(namespaceHierarchy[i]);
			}
			return current;
		}
		
		NamespaceEntity GetTopLevelNamespace(string topLevelName)
		{
			NamespaceEntity tag = (NamespaceEntity)_namespaces[topLevelName];	
			if (null == tag)
			{
				_namespaces[topLevelName] = tag = new NamespaceEntity(null, TypeSystemServices, topLevelName);
			}
			return tag;
		}
		
		override public void Dispose()
		{
			base.Dispose();
			_namespaces.Clear();
		}
	}
}
