#region license
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

import System
import System.IO
import Boo.AntlrParser from Boo.AntlrParser
import Boo.Lang.Compiler.Ast

def WriteNodeTypeEnum(module as Module):
	using writer=OpenFile(GetPath("NodeType.boo")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.WriteLine("""
namespace MetaBoo.Ast

enum NodeType:
	""")
	
		nodes = GetConcreteAstNodes(module)
		for item as TypeDefinition in nodes:
			writer.Write("\t${item.Name}")
			
		writer.Write("""	
""")

def WriteEnum(node as EnumDefinition):
	using writer=OpenFile(GetPathFromNode(node)):
		WriteLicense(writer)
		writer.Write("""
namespace MetaBoo.Ast

enum ${node.Name}:
""")		
		for field as EnumMember in node.Members:
			writer.Write("\t${field.Name}")
			if field.Initializer:
				writer.Write(" = ${field.Initializer.Value}")
			writer.WriteLine()
		writer.Write("""	
""")

def FormatParameterList(fields as List):
	return [
			"${GetParameterName(f)} as ${f.Type}"
			for f as Field in fields
			].Join(", ")
	
def GetParameterName(field as Field):
	name = field.Name
	name = name[0:1].ToLower() + name[1:]
	if name in "namespace", "operator":
		name += "_"
	return name
		
def WriteAssignmentsFromParameters(writer as TextWriter, fields as List):
	for field as Field in fields:
		writer.Write("""
		self.${field.Name} = ${GetParameterName(field)}""")
	
def WriteClassImpl(node as ClassDefinition):
	
	allFields = GetAllFields(node)
	
	using writer=OpenFile(GetPath("Impl/${node.Name}Impl.boo")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.WriteLine("""
namespace MetaBoo.Ast.Impl

import Boo.Lang.Compiler.Ast
import System.Collections
import System.Runtime.Serialization

abstract class ${node.Name}Impl(${join(node.BaseTypes, ', ')}):
""")
	
		for field as Field in node.Members:
			writer.WriteLine("\t${GetPrivateName(field)} as ${field.Type} ")
	
		writer.WriteLine("""
	protected def constructor():
		InitializeFields()
		
	protected def constructor(info as LexicalInfo):
		super(info)
		InitializeFields()
		""")
		
		simpleFields = GetSimpleFields(node)
		if len(simpleFields):
			writer.Write("""
	protected def constructor(${FormatParameterList(simpleFields)}):
		InitializeFields()""")
				
			WriteAssignmentsFromParameters(writer, simpleFields)
			writer.Write("""
			
	protected def constructor(lexicalInfo as LexicalInfo, ${FormatParameterList(simpleFields)}):
		super(lexicalInfo)
		InitializeFields()""")
			WriteAssignmentsFromParameters(writer, simpleFields)
			
		unless IsAbstract(node):
			writer.WriteLine("""
	override NodeType:
		get:
			return NodeType.${node.Name}
			""")
		
			writer.WriteLine("""
	override def Replace(existing as Node, newNode as Node):
		return true if super(existing, newNode)
		""")
			
			for field as Field in allFields:				
				fieldType=ResolveFieldType(field)
				if not fieldType:
					continue
					
				fieldName=GetPrivateName(field)					
				if IsCollection(fieldType):
					collectionItemType = GetCollectionItemType(fieldType)
					writer.WriteLine("""
		if ${fieldName} is not null:
			item = existing as ${collectionItemType};
			if item is not null:
				if ${fieldName}.Replace(item, newNode):
					return true
		""")
				else:
					unless IsEnum(fieldType):
						writer.WriteLine("""
		if ${fieldName} == existing:
			this.${field.Name} = newNode
			return true
		""")
			
			writer.WriteLine("""
		return false
		""")
		
			writer.WriteLine("""
	override def Clone():
			
		clone = FormatterServices.GetUninitializedObject(${node.Name}) as ${node.Name}
		clone._lexicalInfo = _lexicalInfo
		clone._documentation = _documentation
		clone._properties = _properties.Clone()
		""")
			
			for field as Field in allFields:
				fieldType = ResolveFieldType(field)
				fieldName = GetPrivateName(field)
				if fieldType and not IsEnum(fieldType):
					writer.WriteLine("""
		if ${fieldName} is not null:
			clone.${fieldName} = ${fieldName}.Clone()
			clone.${fieldName}.InitializeParent(clone)
		""")
				else:
					writer.WriteLine("""
		clone.${fieldName} = ${fieldName}""")
			
			writer.Write("""			
		return clone
			""")
		
		for field as Field in node.Members:
			writer.WriteLine("""
	${field.Name} as ${field.Type}:
		get:
			return ${GetPrivateName(field)}
	""")
			
			fieldType = ResolveFieldType(field)
			if fieldType and not IsEnum(fieldType):
				writer.WriteLine("""
		set:
			if ${GetPrivateName(field)} != value:
				${GetPrivateName(field)} = value
				if ${GetPrivateName(field)} is not null:
					${GetPrivateName(field)}.InitializeParent(self)""")
						
				if field.Attributes.Contains("LexicalInfo"):
					writer.WriteLine("""
						self.LexicalInfo = value.LexicalInfo
			""")
			else:
				writer.WriteLine("""
		set:
			${GetPrivateName(field)} = value
		""")
		
		writer.WriteLine("""
	private def InitializeFields():
	""")
		
		for field as Field in node.Members:
			if IsCollectionField(field):
				writer.WriteLine("\t\t${GetPrivateName(field)} = ${field.Type}(self)")
			else:
				if field.Attributes.Contains("auto"):
					writer.WriteLine("""
		${GetPrivateName(field)} = ${field.Type}()
		${GetPrivateName(field)}.InitializeParent(this)
			""")
		

def WriteClass(node as ClassDefinition):
	path = GetPathFromNode(node);
	return if File.Exists(path)
	
	using writer=OpenFile(path):
		WriteLicense(writer)
		writer.Write("""
namespace MetaBoo.Ast	
	
class ${node.Name}(MetaBoo.Ast.Impl.${node.Name}Impl):
	def constructor():
		super()
		
	def constructor(lexicalInfo as LexicalInfo):
		super(lexicalInfo)
""")
	
def WriteCollection(node as ClassDefinition):
	path = GetPathFromNode(node)
	return if File.Exists(path)
	
	using writer=OpenFile(path):
		WriteLicense(writer)
		writer.Write("""
namespace Boo.Lang.Compiler.Ast
{
	using System;
	
	[Serializable]
	public class ${node.Name} : Boo.Lang.Compiler.Ast.Impl.${node.Name}Impl
	{
		public ${node.Name}()
		{
		}
		
		public ${node.Name}(Boo.Lang.Compiler.Ast.Node parent) : base(parent)
		{
		}
	}
}
""")

def GetCollectionItemType(node as ClassDefinition):
	attribute = node.Attributes.Get("collection")[0]
	reference as ReferenceExpression = attribute.Arguments[0]
	return reference.Name
	
def ResolveFieldType(field as Field):
	return field.DeclaringType.DeclaringType.Members[(field.Type as SimpleTypeReference).Name]
	
def GetResultingTransformerNode(node as ClassDefinition):
	for subclass in "Statement", "Expression", "TypeReference":
		if IsSubclassOf(node, subclass):
			return subclass
	return node.Name
	
def IsSubclassOf(node as ClassDefinition, typename as string):
	for typeref as SimpleTypeReference in node.BaseTypes:
		if typename == typeref.Name:
			return true
		baseType = node.DeclaringType.Members[typeref.Name]
		if baseType and IsSubclassOf(baseType, typename):
			return true
	return false

def WriteCollectionImpl(node as ClassDefinition):
	path = GetPath("Impl/${node.Name}Impl.cs")
	using writer=OpenFile(path):
		WriteLicense(writer)
		WriteWarning(writer)
		
		itemType = "Boo.Lang.Compiler.Ast." + GetCollectionItemType(node)
		writer.Write("""
namespace Boo.Lang.Compiler.Ast.Impl
{
	using System;
	using Boo.Lang.Compiler.Ast;
	
	[Serializable]
	public class ${node.Name}Impl : NodeCollection
	{
		protected ${node.Name}Impl()
		{
		}
		
		protected ${node.Name}Impl(Node parent) : base(parent)
		{
		}
		
		public ${itemType} this[int index]
		{
			get
			{
				return (${itemType})InnerList[index];
			}
		}

		public void Add(${itemType} item)
		{
			base.AddNode(item);			
		}
		
		public void Extend(params ${itemType}[] items)
		{
			base.AddNodes(items);			
		}
		
		public void Extend(System.Collections.ICollection items)
		{
			foreach (${itemType} item in items)
			{
				base.AddNode(item);
			}
		}
		
		public void ExtendWithClones(System.Collections.ICollection items)
		{
			foreach (${itemType} item in items)
			{
				base.AddNode(item.CloneNode());
			}
		}
		
		public void Insert(int index, ${itemType} item)
		{
			base.InsertNode(index, item);
		}
		
		public bool Replace(${itemType} existing, ${itemType} newItem)
		{
			return base.ReplaceNode(existing, newItem);
		}
		
		public new ${itemType}[] ToArray()
		{
			return (${itemType}[])InnerList.ToArray(typeof(${itemType}));
		}
	}
}
""")

def OpenFile(fname as string):	
	print(fname)
	return StreamWriter(fname, false, System.Text.Encoding.UTF8)
	
def GetPath(fname as string):
	return Path.Combine("src/MetaBoo/Ast", fname)
	
def GetPathFromNode(node as TypeMember):
	return GetPath("${node.Name}.boo")
	
def IsCollection(node as TypeMember):
	return node.Attributes.Contains("collection")
	
def IsCollectionField(field as Field):
	return (field.Type as SimpleTypeReference).Name.EndsWith("Collection")
	
def IsEnum(node as TypeMember):
	return NodeType.EnumDefinition == node.NodeType
	
def IsAbstract(member as TypeMember):
	return member.IsModifierSet(TypeMemberModifiers.Abstract)
	
def GetConcreteAstNodes(module as Module):
	nodes = []
	for member in module.Members:
		nodes.Add(member) if IsConcreteAstNode(member)
	return nodes	
	
def IsConcreteAstNode(member as TypeMember):
	return not (IsCollection(member) or IsEnum(member) or IsAbstract(member))
	
def WriteWarning(writer as TextWriter):
	writer.Write(
"""
//
// DO NOT EDIT THIS FILE!
//
// This file was generated automatically by
// astgen.boo on ${date.Now}
//
""")
	
def WriteLicense(writer as TextWriter):
	writer.Write(
"""#region license
// boo - an extensible programming language for the CLI
// Copyright (C) 2004 Rodrigo B. de Oliveira
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, 
// publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
// OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Contact Information
//
// mailto:rbo@acm.org
#endregion
""")

def WriteVisitor(module as Module):
	using writer=OpenFile(GetPath("INodeVisitor.boo")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.Write(
"""
namespace MetaBoo.Ast


import System
	
interface INodeVisitor:
""")
		for member as TypeMember in module.Members:
			if IsConcreteAstNode(member):
				writer.WriteLine("\tdef On${member.Name}(node as ${member.Name} )")


def GetTypeHierarchy(item as ClassDefinition):
	types = []
	ProcessTypeHierarchy(types, item)
	return types
	
def ProcessTypeHierarchy(types as List, item as ClassDefinition):
	module as Module = item.ParentNode
	for baseTypeRef as SimpleTypeReference in item.BaseTypes:
		if baseType = module.Members[baseTypeRef.Name]:
			ProcessTypeHierarchy(types, baseType)
	types.Add(item)
	
def GetPrivateName(field as Field):
	name = field.Name
	return "_" + name[0:1].ToLower() + name[1:]
	
def GetSimpleFields(node as ClassDefinition):
	
	fields = []
	for field as Field in node.Members:
		if IsCollectionField(field) or field.Attributes.Contains("auto"):
			continue
		fields.Add(field)
	return fields
	
def GetAllFields(node as ClassDefinition):
	fields = []
	module as Module = node.ParentNode
	
	for item as TypeDefinition in GetTypeHierarchy(node):
		for field as Field in item.Members:
			fields.Add(field)
	return fields

def GetVisitableFields(item as ClassDefinition):
	fields = []
	
	module as Module = item.ParentNode
	
	for item as TypeDefinition in GetTypeHierarchy(item):	
		for field as Field in item.Members:
			type = ResolveFieldType(field)
			if type:
				if not IsEnum(type):
					fields.Add(field)
	
	return fields

def WriteDepthFirstVisitor(writer as TextWriter, item as ClassDefinition):
	fields = GetVisitableFields(item)
	
	if len(fields):
		writer.WriteLine("""
		public virtual void On${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{				
			if (Enter${item.Name}(node))
			{""")
			
		for field as Field in fields:
			writer.WriteLine("\t\t\t\tSwitch(node.${field.Name});")
			
		writer.Write(
"""				Leave${item.Name}(node);
			}
		}
			
		public virtual bool Enter${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{
			return true;
		}
		
		public virtual void Leave${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{
		}
			""")
	else:
		writer.Write("""
		public virtual void On${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{
		}
			""")

def WriteDepthFirstVisitor(module as Module):
	using writer=OpenFile(GetPath("DepthFirstVisitor.boo")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.Write(
"""
namespace Boo.Lang.Compiler.Ast
{
	using System;
	
	public class DepthFirstSwitcher : IAstSwitcher
	{
		public bool Switch(Node node)
		{			
			if (null != node)
			{
				try
				{
					node.Switch(this);
					return true;
				}
				catch (Boo.Lang.Compiler.CompilerError)
				{
					throw;
				}
				catch (Exception error)
				{
					throw Boo.Lang.Compiler.CompilerErrorFactory.InternalError(node, error);
				}
			}
			return false;
		}
		
		public void Switch(Node[] array, NodeType nodeType)
		{
			foreach (Node node in array)
			{
				if (node.NodeType == nodeType)
				{
					Switch(node);
				}
			}
		}
		
		public bool Switch(NodeCollection collection, NodeType nodeType)
		{
			if (null != collection)
			{
				Switch(collection.ToArray(), nodeType);
				return true;
			}
			return false;
		}
		
		public void Switch(Node[] array)
		{
			foreach (Node node in array)
			{
				Switch(node);
			}
		}
		
		public bool Switch(NodeCollection collection)
		{
			if (null != collection)
			{
				Switch(collection.ToArray());
				return true;
			}
			return false;
		}
		""")
		
		for member as TypeMember in module.Members:
			if IsConcreteAstNode(member):
				WriteDepthFirstVisitor(writer, member)
		
		writer.Write("""
	}
}
""")

start = date.Now

module = BooParser.ParseFile("ast.model.boo").Modules[0]

WriteVisitor(module)
WriteDepthFirstVisitor(module)
WriteNodeTypeEnum(module)
for member as TypeMember in module.Members:

	if member.Attributes.Contains("ignore"):
		continue
		
	if IsEnum(member):
		WriteEnum(member)
	else:
		if IsCollection(member):
			WriteCollection(member)
			WriteCollectionImpl(member)
		else:
			WriteClass(member)
			WriteClassImpl(member)
			
stop = date.Now
print("ast classes generated in ${stop-start}.")
