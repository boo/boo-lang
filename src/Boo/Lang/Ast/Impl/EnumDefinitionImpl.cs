﻿#region license
// boo - an extensible programming language for the CLI
// Copyright (C) 2004 Rodrigo B. de Oliveira
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// As a special exception, if you link this library with other files to
// produce an executable, this library does not by itself cause the
// resulting executable to be covered by the GNU General Public License.
// This exception does not however invalidate any other reasons why the
// executable file might be covered by the GNU General Public License.
//
// Contact Information
//
// mailto:rbo@acm.org
#endregion

//
// DO NOT EDIT THIS FILE!
//
// This file was generated automatically by
// astgenerator.boo on 3/26/2004 11:12:00 AM
//

namespace Boo.Lang.Ast.Impl
{	
	using Boo.Lang.Ast;
	using System.Collections;
	using System.Runtime.Serialization;
	
	[System.Serializable]
	public abstract class EnumDefinitionImpl : TypeDefinition
	{


		protected EnumDefinitionImpl()
		{
			InitializeFields();
		}
		
		protected EnumDefinitionImpl(LexicalInfo info) : base(info)
		{
			InitializeFields();
		}
		

		new public EnumDefinition CloneNode()
		{
			return Clone() as EnumDefinition;
		}

		override public NodeType NodeType
		{
			get
			{
				return NodeType.EnumDefinition;
			}
		}
		
		override public void Switch(IAstTransformer transformer, out Node resultingNode)
		{
			EnumDefinition thisNode = (EnumDefinition)this;
			EnumDefinition resultingTypedNode = thisNode;
			transformer.OnEnumDefinition(thisNode, ref resultingTypedNode);
			resultingNode = resultingTypedNode;
		}

		override public bool Replace(Node existing, Node newNode)
		{
			if (base.Replace(existing, newNode))
			{
				return true;
			}

			if (_attributes != null)
			{
				Attribute item = existing as Attribute;
				if (null != item)
				{
					Attribute newItem = (Attribute)newNode;
					if (_attributes.Replace(item, newItem))
					{
						return true;
					}
				}
			}

			if (_members != null)
			{
				TypeMember item = existing as TypeMember;
				if (null != item)
				{
					TypeMember newItem = (TypeMember)newNode;
					if (_members.Replace(item, newItem))
					{
						return true;
					}
				}
			}

			if (_baseTypes != null)
			{
				TypeReference item = existing as TypeReference;
				if (null != item)
				{
					TypeReference newItem = (TypeReference)newNode;
					if (_baseTypes.Replace(item, newItem))
					{
						return true;
					}
				}
			}

			return false;
		}

		override public object Clone()
		{
			EnumDefinition clone = FormatterServices.GetUninitializedObject(typeof(EnumDefinition)) as EnumDefinition;
			clone._lexicalInfo = _lexicalInfo;
			clone._documentation = _documentation;
			clone._properties = _properties.Clone() as Hashtable;
			

			clone._modifiers = _modifiers;

			clone._name = _name;

			if (null != _attributes)
			{
				clone._attributes = _attributes.Clone() as AttributeCollection;
				clone._attributes.InitializeParent(clone);
			}

			if (null != _members)
			{
				clone._members = _members.Clone() as TypeMemberCollection;
				clone._members.InitializeParent(clone);
			}

			if (null != _baseTypes)
			{
				clone._baseTypes = _baseTypes.Clone() as TypeReferenceCollection;
				clone._baseTypes.InitializeParent(clone);
			}
			
			return clone;
		}
			
		private void InitializeFields()
		{

		}
	}
}
