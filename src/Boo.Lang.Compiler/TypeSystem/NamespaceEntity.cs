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
	using System.Collections.Generic;

	using TypeList = System.Collections.Generic.List<System.Type>;
	using ITypeList = System.Collections.Generic.List<IType>;
	using IEntityList = System.Collections.Generic.List<IEntity>;
	using ModuleList = System.Collections.Generic.List<ModuleEntity>;
	using TypeListMap = System.Collections.Generic.Dictionary<System.Reflection.Assembly, System.Collections.Generic.List<System.Type> >;
	using IEntityMap = System.Collections.Generic.Dictionary<string, IEntity>;
	using NamespaceEntityMap = System.Collections.Generic.Dictionary<string, NamespaceEntity>;

	public class NamespaceEntity : IEntity, INamespace
	{		
		TypeSystemServices _typeSystemServices;
		
		INamespace _parent;
		
		string _name;
		
		TypeListMap _assemblies;

		NamespaceEntityMap _childrenNamespaces;

		ModuleList _internalModules;
		
		ITypeList _externalModules;
		
		public NamespaceEntity(INamespace parent, TypeSystemServices tagManager, string name)
		{			
			_parent = parent;
			_typeSystemServices = tagManager;
			_name = name;
			_childrenNamespaces = new NamespaceEntityMap();
			_assemblies = new TypeListMap();
			_internalModules = new ModuleList();
			_externalModules = new ITypeList();
		}
		
		public string Name
		{
			get
			{
				return _name;
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
			TypeList types = null;
			System.Reflection.Assembly assembly = type.Assembly;
			if (!_assemblies.TryGetValue(assembly, out types))
			{
				types = new TypeList();
				_assemblies[assembly] = types;
			}
			types.Add(type);
			
			if (IsModule(type))
			{
				_externalModules.Add(_typeSystemServices.Map(type));
			}
		}
		
		bool IsModule(Type type)
		{
			return System.Attribute.IsDefined(type, typeof(Boo.Lang.ModuleAttribute));
		}		
		
		public void AddModule(Boo.Lang.Compiler.TypeSystem.ModuleEntity module)
		{
			_internalModules.Add(module);
		}
		
		public IEntity[] GetMembers()
		{
			IEntityList members = new IEntityList();
			foreach (IEntity e in _childrenNamespaces.Values)
			{
				members.Add(e);
			}
			foreach (TypeList types in _assemblies.Values)
			{
				foreach (Type type in types)
				{
					members.Add(_typeSystemServices.Map(type));
				}
			}
			return members.ToArray();
		}
		
		public NamespaceEntity GetChildNamespace(string name)
		{
			NamespaceEntity tag = null;
			if (!_childrenNamespaces.TryGetValue(name, out tag))
			{				
				tag = new NamespaceEntity(this, _typeSystemServices, _name + "." + name);
				_childrenNamespaces[name] = tag;
			}
			return tag;
		}
		
		internal bool Resolve(Boo.Lang.List targetList, string name, System.Reflection.Assembly assembly, EntityType flags)
		{
			NamespaceEntity tag = null; 
			if (_childrenNamespaces.TryGetValue(name, out tag))
			{
				targetList.Add(new AssemblyQualifiedNamespaceEntity(assembly, tag));
				return true;
			}
			
			TypeList types = _assemblies[assembly];
			
			bool found = false;
			if (null != types)
			{
				foreach (Type type in types)
				{
					if (name == type.Name)
					{
						targetList.Add(_typeSystemServices.Map(type));
						found = true;
						break;
					}
				}
				
				foreach (ExternalType external in _externalModules)
				{
					if (external.ActualType.Assembly == assembly)
					{
						found |= external.Resolve(targetList, name, flags); 
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
		
		public bool Resolve(Boo.Lang.List targetList, string name, EntityType flags)
		{
			NamespaceEntity tag = null;
			if (_childrenNamespaces.TryGetValue(name, out tag))
			{
				targetList.Add(tag);
				return true;
			}
			
			bool found = false;
			if (!ResolveInternalType(targetList, name, flags))
			{
				found = ResolveExternalType(targetList, name);
				found |= ResolveExternalModules(targetList, name, flags);
			}
			return found;
		}
		
		bool ResolveInternalType(Boo.Lang.List targetList, string name, EntityType flags)
		{
			foreach (ModuleEntity ns in _internalModules)
			{
				ns.ResolveMember(targetList, name, flags);
			}
			return false;
		}
		
		bool ResolveExternalType(Boo.Lang.List targetList, string name)
		{			
			foreach (TypeList types in _assemblies.Values)
			{				
				foreach (Type type in types)
				{
					if (name == type.Name)
					{
						targetList.Add(_typeSystemServices.Map(type));
						return true;
					}
				}
			}
			return false;
		}

		bool ResolveExternalModules(Boo.Lang.List targetList, string name, EntityType flags)
		{
			bool found = false;
			foreach (INamespace ns in _externalModules)
			{
				found |= ns.Resolve(targetList, name, flags);
			}
			return found;
		}
		
		override public string ToString()
		{
			return _name;
		}
	}
}
