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
	using System.Collections;
	using System.Reflection;

	public class NamespaceEntity : IEntity, INamespace
	{		
		TypeSystemServices _typeSystemServices;
		
		INamespace _parent;
		
		string _name;
		
		Hashtable _assemblies;
		
		Hashtable _childrenNamespaces;
		
		List _internalModules;
		
		List _externalModules;
		
		public NamespaceEntity(INamespace parent, TypeSystemServices tagManager, string name)
		{			
			_parent = parent;
			_typeSystemServices = tagManager;
			_name = name;
			_assemblies = new Hashtable();
			_childrenNamespaces = new Hashtable();
			_assemblies = new Hashtable();
			_internalModules = new List();
			_externalModules = new List();
		}
		
		public string Name
		{
			get
			{
				return GetLastPart(_name);
			}
		}
		
		public string FullName
		{
			get
			{
				return _name;
			}
		}
		
		public EntityType EntityType
		{
			get
			{
				return EntityType.Namespace;
			}
		}
		
		public void Add(Type type)
		{
			Assembly assembly = type.Assembly;
			List types = (List)_assemblies[assembly];
			if (null == types)
			{
				types = new List();
				_assemblies[assembly] = types;
			}
			types.Add(type);
			
			if (_typeSystemServices.IsModule(type))
			{
				_externalModules.Add(_typeSystemServices.Map(type));
			}
		}
		
		public void AddModule(ModuleEntity module)
		{
			_internalModules.Add(module);
		}
		
		public IEntity[] GetMembers()
		{
			List members = new List();
			members.Extend(_childrenNamespaces.Values);
			foreach (List types in _assemblies.Values)
			{
				foreach (Type type in types)
				{
					members.Add(_typeSystemServices.Map(type));
				}
			}
			return (IEntity[])members.ToArray(typeof(IEntity));
		}
		
		public NamespaceEntity GetChildNamespace(string name)
		{
			NamespaceEntity tag = (NamespaceEntity)_childrenNamespaces[name];
			if (null == tag)
			{				
				tag = new NamespaceEntity(this, _typeSystemServices, _name + "." + name);
				_childrenNamespaces[name] = tag;
			}
			return tag;
		}
		
		internal bool Resolve(List targetList, string name, Assembly assembly, EntityType flags)
		{
			NamespaceEntity entity = (NamespaceEntity)_childrenNamespaces[name];
			if (null != entity)
			{
				targetList.Add(new AssemblyQualifiedNamespaceEntity(assembly, entity));
				return true;
			}
			
			List types = (List)_assemblies[assembly];
			
			bool found = false;
			if (null != types)
			{
				found = ResolveType(targetList, name, types);
				
				foreach (ExternalType external in _externalModules)
				{
					if (external.ActualType.Assembly == assembly)
					{
						if (external.Resolve(targetList, name, flags)) found = true; 
					}
				}
			}
			return found;
		}
		
		public INamespace ParentNamespace
		{
			get
			{
				return _parent;
			}
		}
		
		public bool Resolve(List targetList, string name, EntityType flags)
		{	
			IEntity tag = (IEntity)_childrenNamespaces[name];
			if (null != tag)
			{
				targetList.Add(tag);
				return true;
			}
			
			bool found = ResolveInternalType(targetList, name, flags);
			if (found) return true;
			
			found = ResolveExternalType(targetList, name);
			if (ResolveExternalModules(targetList, name, flags)) found = true;
			return found;
		}
		
		bool ResolveInternalType(List targetList, string name, EntityType flags)
		{
			bool found = false;
			foreach (ModuleEntity ns in _internalModules)
			{
				if (ns.ResolveMember(targetList, name, flags)) found = true;
			}
			return found;
		}
		
		bool ResolveExternalType(List targetList, string name)
		{			
			foreach (List types in _assemblies.Values)
			{
				if (ResolveType(targetList, name, types)) return true;
			}
			return false;
		}

		private bool ResolveType(List targetList, string name, IEnumerable types)
		{
			bool found = false;
			foreach (Type type in types)
			{
				if (name == TypeName(type))
				{
					targetList.Add(_typeSystemServices.Map(type));
					found = true;
					// Can't return right away, since we can have several types
					// with the same name but different number of generic arguments. 
				}
			}
			return found;
		}

		private static string TypeName(Type type)
		{
			if (!type.IsGenericTypeDefinition) return type.Name;
			string name = type.Name;
			return name.Substring(0, name.LastIndexOf('`'));
		}

		bool ResolveExternalModules(List targetList, string name, EntityType flags)
		{
			bool found = false;
			foreach (INamespace ns in _externalModules)
			{
				if (ns.Resolve(targetList, name, flags)) found = true;
			}
			return found;
		}
		
		override public string ToString()
		{
			return _name;
		}

		private string GetLastPart(string name)
		{
			int index = name.LastIndexOf('.');
			return index < 0 ? name : name.Substring(index+1);
		}
	}
}
