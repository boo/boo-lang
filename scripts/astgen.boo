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

import System
import System.IO
import Boo.Lang.Useful.IO from Boo.Lang.Useful
import Boo.Lang.Compiler
import Boo.Lang.Compiler.Pipelines
import Boo.Lang.Compiler.Ast

class LicenseWriter:
	static _license = TextFile.ReadFile("notice.txt")
	
	static def WriteLicenseNotice(writer as TextWriter):
		writer.Write(_license)	

def WriteNodeTypeEnum(module as Module):
	using writer=OpenFile(GetPath("NodeType.cs")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.WriteLine("""
namespace Boo.Lang.Compiler.Ast
{
	using System;
	
	[Serializable]
	public enum NodeType
	{""")
	
		nodes = GetConcreteAstNodes(module)
		last = nodes[-1]
		for item as TypeDefinition in nodes:
			writer.Write("\t\t${item.Name}")
			if item is not last:
				writer.WriteLine(", ")
	
		writer.Write("""
	}
}""")

def WriteEnum(node as EnumDefinition):
	using writer=OpenFile(GetPathFromNode(node)):
		WriteLicense(writer)
		writer.Write("""
namespace Boo.Lang.Compiler.Ast
{
	using System;

	[Serializable]
""")
		if node.Name.EndsWith("Modifiers"):
			writer.WriteLine("	[Flags]")
		writer.Write("""	public enum ${node.Name}
	{	
""")
		last = node.Members[-1]
		for field as EnumMember in node.Members:
			writer.Write("\t\t${field.Name}")
			if field.Initializer:
				writer.Write(" = ${field.Initializer.Value}")
			if field is not last:
				writer.Write(",")
			writer.WriteLine()

		writer.Write("""
	}
}
""")

def FormatParameterList(fields as List):
	return join(
			"${field.Type} ${GetParameterName(field)}"
			for field as Field in fields,
			", ")
	
def GetParameterName(field as Field):
	name = field.Name
	name = name[0:1].ToLower() + name[1:]
	if name in "namespace", "operator":
		name += "_"
	return name
		
def WriteAssignmentsFromParameters(writer as TextWriter, fields as List):
	for field as Field in fields:
		writer.Write("""
			${field.Name} = ${GetParameterName(field)};""")
			
def WriteMatchesImpl(writer as TextWriter, node as ClassDefinition):
	writer.WriteLine("""
		override public bool Matches(Node node)
		{	
			${node.Name} other = node as ${node.Name};
			if (null == other) return false;""")
	
	for field as Field in GetAllFields(node):
		fieldName = GetPrivateName(field)
		fieldType = ResolveFieldType(field)
		if fieldType is null or IsEnum(fieldType):
			writer.WriteLine("""
			if (${fieldName} != other.${fieldName}) return false;""")
			continue
		writer.WriteLine("""
			if (!Node.Matches(${fieldName}, other.${fieldName})) return false;""")
	
	writer.WriteLine("""
			return true;
		}
	""");
	
def WriteClassImpl(node as ClassDefinition):
	
	allFields = GetAllFields(node)
	
	using writer=OpenFile(GetPath("Impl/${node.Name}Impl.cs")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.WriteLine("""
namespace Boo.Lang.Compiler.Ast.Impl
{	
	using Boo.Lang.Compiler.Ast;
	using System.Collections;
	using System.Runtime.Serialization;
	
	[System.Serializable]
	public abstract class ${node.Name}Impl : ${join(node.BaseTypes, ', ')}
	{
""")
	
		for field as Field in node.Members:
			writer.WriteLine("\t\tprotected ${field.Type} ${GetPrivateName(field)};");
	
		writer.WriteLine("""
		protected ${node.Name}Impl()
		{
			InitializeFields();
		}
		
		protected ${node.Name}Impl(LexicalInfo info) : base(info)
		{
			InitializeFields();
		}
		""")
		
		simpleFields = GetSimpleFields(node)
		if len(simpleFields):
			writer.Write("""
		protected ${node.Name}Impl(${FormatParameterList(simpleFields)})
		{
			InitializeFields();""")
				
			WriteAssignmentsFromParameters(writer, simpleFields)
			writer.Write("""
		}
			
		protected ${node.Name}Impl(LexicalInfo lexicalInfo, ${FormatParameterList(simpleFields)}) : base(lexicalInfo)
		{
			InitializeFields();""")
			WriteAssignmentsFromParameters(writer, simpleFields)	
			writer.Write("""
		}
			""")
			
		writer.WriteLine("""
		new public ${node.Name} CloneNode()
		{
			return Clone() as ${node.Name};
		}""")
		
		unless IsAbstract(node):
			writer.WriteLine("""
		override public NodeType NodeType
		{
			get
			{
				return NodeType.${node.Name};
			}
		}""")
		
			WriteMatchesImpl(writer, node)
		
			writer.WriteLine("""
		override public bool Replace(Node existing, Node newNode)
		{
			if (base.Replace(existing, newNode))
			{
				return true;
			}""")
			
			for field as Field in allFields:				
				fieldType=ResolveFieldType(field)
				if not fieldType:
					continue
					
				fieldName=GetPrivateName(field)					
				if IsCollection(fieldType):
					collectionItemType = GetCollectionItemType(fieldType)
					writer.WriteLine("""
			if (${fieldName} != null)
			{
				${collectionItemType} item = existing as ${collectionItemType};
				if (null != item)
				{
					${collectionItemType} newItem = (${collectionItemType})newNode;
					if (${fieldName}.Replace(item, newItem))
					{
						return true;
					}
				}
			}""")
				else:
					unless IsEnum(fieldType):
						writer.WriteLine("""
			if (${fieldName} == existing)
			{
				this.${field.Name} = (${field.Type})newNode;
				return true;
			}""")
			
			writer.WriteLine("""
			return false;
		}""")
		
			writer.WriteLine("""
		override public object Clone()
		{
			${node.Name} clone = FormatterServices.GetUninitializedObject(typeof(${node.Name})) as ${node.Name};
			clone._lexicalInfo = _lexicalInfo;
			clone._endSourceLocation = _endSourceLocation;
			clone._documentation = _documentation;
			//clone._entity = _entity;
			clone._annotations = (Hashtable)_annotations.Clone();
			""")
			
			if IsExpression(node):
				writer.WriteLine("""
			clone._expressionType = _expressionType;
			""");
			
			for field as Field in allFields:
				fieldType = ResolveFieldType(field)
				fieldName = GetPrivateName(field)
				if fieldType and not IsEnum(fieldType):
					writer.WriteLine("""
			if (null != ${fieldName})
			{
				clone.${fieldName} = ${fieldName}.Clone() as ${field.Type};
				clone.${fieldName}.InitializeParent(clone);
			}""")
				else:
					writer.WriteLine("""
			clone.${fieldName} = ${fieldName};""")
			
			writer.Write("""			
			return clone;
		}
			""")
			
			writer.WriteLine("""
		override internal void ClearTypeSystemBindings()
		{
			_annotations.Clear();
			//_entity = null;
			""")
			
			if IsExpression(node):
				writer.WriteLine("""
			_expressionType = null;
			""");
			
			for field as Field in allFields:
				fieldType = ResolveFieldType(field)
				fieldName = GetPrivateName(field)
				if fieldType and not IsEnum(fieldType):
					writer.WriteLine("""
			if (null != ${fieldName})
			{
				${fieldName}.ClearTypeSystemBindings();
			}""")
			
			writer.WriteLine("""
		}""")
		
		for field as Field in node.Members:
			if field.Name == "Name":
				writer.Write("""
		[System.Xml.Serialization.XmlAttribute]""")
			elif field.Name == "Modifiers":
				writer.Write("""
		[System.Xml.Serialization.XmlAttribute,
		System.ComponentModel.DefaultValue(${field.Type}.None)]""")
			else:
				writer.Write("""
		[System.Xml.Serialization.XmlElement]""")

			writer.WriteLine("""
		public ${field.Type} ${field.Name}
		{
			get
			{
				return ${GetPrivateName(field)};
			}
			""")
			
			fieldType = ResolveFieldType(field)
			if fieldType and not IsEnum(fieldType):
				writer.WriteLine("""
			set
			{
				if (${GetPrivateName(field)} != value)
				{
					${GetPrivateName(field)} = value;
					if (null != ${GetPrivateName(field)})
					{
						${GetPrivateName(field)}.InitializeParent(this);""")
						
				if field.Attributes.Contains("LexicalInfo"):
					writer.WriteLine("""
						LexicalInfo = value.LexicalInfo;""")
						
				writer.WriteLine("""
					}
				}
			}
			""")
			else:
				writer.WriteLine("""
			set
			{
				${GetPrivateName(field)} = value;
			}""")
			
			writer.WriteLine("""
		}
		""")
		
		writer.WriteLine("""
		private void InitializeFields()
		{""")
		
		for field as Field in node.Members:
			if IsCollectionField(field):
				writer.WriteLine("\t\t\t${GetPrivateName(field)} = new ${field.Type}(this);")
			else:
				if field.Attributes.Contains("auto"):
					writer.WriteLine("""
			${GetPrivateName(field)} = new ${field.Type}();
			${GetPrivateName(field)}.InitializeParent(this);
			""")
		
		writer.WriteLine("""
		}
	}
}""")
		

def WriteClass(node as ClassDefinition):
	path = GetPathFromNode(node);
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
		
		public ${node.Name}(LexicalInfo lexicalInfo) : base(lexicalInfo)
		{
		}
	}
}
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
	
def IsExpression(node as ClassDefinition):
	return IsSubclassOf(node, "Expression")
	
def GetResultingTransformerNode(node as ClassDefinition):
	for subclass in "Statement", "Expression", "TypeReference":
		if IsSubclassOf(node, subclass):
			return subclass
	return node.Name
	
def IsSubclassOf(node as ClassDefinition, typename as string) as bool:
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
	[Boo.Lang.EnumeratorItemType(typeof(${itemType}))]
	public class ${node.Name}Impl : NodeCollection
	{
		protected ${node.Name}Impl()
		{
		}
		
		protected ${node.Name}Impl(Node parent) : base(parent)
		{
		}
		
		protected ${node.Name}Impl(Node parent, Boo.Lang.List list) : base(parent, list)
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
		
		public void ReplaceAt(int index, ${itemType} newItem)
		{
			base.ReplaceAt(index, newItem);
		}
		
		public ${itemType}Collection PopRange(int begin)
		{
			return new ${itemType}Collection(_parent, InnerList.PopRange(begin));
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
	return StreamWriter(fname)//, false, System.Text.Encoding.UTF8)
	
def GetPath(fname as string):
	return Path.Combine("src/Boo.Lang.Compiler/Ast", fname)
	
def GetPathFromNode(node as TypeMember):
	return GetPath("${node.Name}.cs")
	
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
// astgenerator.boo on ${date.Now}
//
""")
	
def WriteLicense(writer as TextWriter):
	LicenseWriter.WriteLicenseNotice(writer)

def WriteDepthFirstTransformer(module as Module):
	
	using writer=OpenFile(GetPath("DepthFirstTransformer.cs")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.Write("""
namespace Boo.Lang.Compiler.Ast
{
	using System;
	
	public class DepthFirstTransformer : IAstVisitor
	{
	
		protected Node _resultingNode = null;
	
	""")
	
		for item as TypeMember in module.Members:
			continue unless IsConcreteAstNode(item)
			
			switchableFields = GetAcceptableFields(item)
			resultingNodeType = GetResultingTransformerNode(item)
			
			writer.WriteLine("""
		public virtual void On${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{""")
		
			if len(switchableFields):
				writer.WriteLine("""
			if (Enter${item.Name}(node))
			{""")
				for field as Field in switchableFields:
					if IsCollectionField(field):
						writer.WriteLine("""
				Visit(node.${field.Name});""")
					else:
						writer.WriteLine("""
				${field.Type} current${field.Name}Value = node.${field.Name};
				if (null != current${field.Name}Value)
				{			
					${field.Type} newValue = (${field.Type})VisitNode(current${field.Name}Value);
					if (!object.ReferenceEquals(newValue, current${field.Name}Value))
					{
						node.${field.Name} = newValue;
					}
				}""")
				
				writer.WriteLine("""
				Leave${item.Name}(node);
			}""")
			
			writer.WriteLine("""
		}""")
		
			if len(switchableFields):
				writer.WriteLine("""				
		public virtual bool Enter${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{
			return true;
		}
		
		public virtual void Leave${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{
		}""")
		
		writer.WriteLine("""
		
		protected void RemoveCurrentNode()
		{
			_resultingNode = null;
		}
		
		protected void ReplaceCurrentNode(Node replacement)
		{
			_resultingNode = replacement;
		}
		
		protected virtual void OnNode(Node node)
		{
			node.Accept(this);
		}
		
		public virtual Node VisitNode(Node node)
		{
			if (null != node)
			{
				try
				{
					Node saved = _resultingNode;
					_resultingNode = node;
					OnNode(node);
					Node result = _resultingNode;
					_resultingNode = saved;
					return result;
				}
				catch (Boo.Lang.Compiler.CompilerError)
				{
					throw;
				}
				catch (Exception error)
				{
					OnError(node, error);
				}
			}
			return null;
		}
		
		protected virtual void OnError(Node node, Exception error)
		{
			throw Boo.Lang.Compiler.CompilerErrorFactory.InternalError(node, error);
		}
		
		public Node Visit(Node node)
		{
			return VisitNode(node);
		}
		
		public Expression Visit(Expression node)
		{
			return (Expression)VisitNode(node);
		}
		
		public Statement Visit(Statement node)
		{
			return (Statement)VisitNode(node);
		}
		
		public bool Visit(NodeCollection collection)
		{
			if (null != collection)
			{
				int removed = 0;
				
				Node[] nodes = collection.ToArray();
				for (int i=0; i<nodes.Length; ++i)
				{					
					Node currentNode = nodes[i];
					Node resultingNode = VisitNode(currentNode);
					if (currentNode != resultingNode)
					{
						int actualIndex = i-removed;
						if (null == resultingNode)
						{
							++removed;
							collection.RemoveAt(actualIndex);
						}
						else
						{
							collection.ReplaceAt(actualIndex, resultingNode);
						}
					}
				}
				return true;
			}
			return false;
		}
	}
}
""")

def WriteVisitor(module as Module):
	using writer=OpenFile(GetPath("IAstVisitor.cs")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.Write(
"""
namespace Boo.Lang.Compiler.Ast
{
	using System;
	
	public interface IAstVisitor
	{
""")
		for member as TypeMember in module.Members:
			if IsConcreteAstNode(member):
				writer.WriteLine("\t\tvoid On${member.Name}(${member.Name} node);")

		writer.Write(
"""
	}
}
""")

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
	
	return [field
			for field as Field in node.Members
			unless IsCollectionField(field) or field.Attributes.Contains("auto")]
	
def GetAllFields(node as ClassDefinition):
	fields = []
	
	for item as TypeDefinition in GetTypeHierarchy(node):
		fields.Extend(item.Members)
			
	return fields

def GetAcceptableFields(item as ClassDefinition):
	
	fields = []
	
	for item as TypeDefinition in GetTypeHierarchy(item):	
		for field as Field in item.Members:
			type = ResolveFieldType(field)
			fields.Add(field) if type and not IsEnum(type)
	
	return fields

def WriteDepthFirstAccept(writer as TextWriter, item as ClassDefinition):
	fields = GetAcceptableFields(item)
	
	if len(fields):
		writer.WriteLine("""
		public virtual void On${item.Name}(Boo.Lang.Compiler.Ast.${item.Name} node)
		{				
			if (Enter${item.Name}(node))
			{""")
			
		for field as Field in fields:
			writer.WriteLine("\t\t\t\tVisit(node.${field.Name});")
			
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
	using writer=OpenFile(GetPath("DepthFirstVisitor.cs")):
		WriteLicense(writer)
		WriteWarning(writer)
		writer.Write(
"""
namespace Boo.Lang.Compiler.Ast
{
	using System;
	
	public class DepthFirstVisitor : IAstVisitor
	{
		public bool Visit(Node node)
		{			
			if (null != node)
			{
				try
				{
					node.Accept(this);
					return true;
				}
				catch (Boo.Lang.Compiler.CompilerError)
				{
					throw;
				}
				catch (Exception error)
				{
					OnError(node, error);
				}
			}
			return false;
		}
		
		protected virtual void OnError(Node node, Exception error)
		{
			throw Boo.Lang.Compiler.CompilerErrorFactory.InternalError(node, error);
		}
		
		public void Visit(Node[] array, NodeType nodeType)
		{
			foreach (Node node in array)
			{
				if (node.NodeType == nodeType)
				{
					Visit(node);
				}
			}
		}
		
		public bool Visit(NodeCollection collection, NodeType nodeType)
		{
			if (null != collection)
			{
				Visit(collection.ToArray(), nodeType);
				return true;
			}
			return false;
		}
		
		public void Visit(Node[] array)
		{
			foreach (Node node in array)
			{
				Visit(node);
			}
		}
		
		public bool Visit(NodeCollection collection)
		{
			if (null != collection)
			{
				Visit(collection.ToArray());
				return true;
			}
			return false;
		}
		""")
		
		for member as TypeMember in module.Members:
			if IsConcreteAstNode(member):
				WriteDepthFirstAccept(writer, member)
		
		writer.Write("""
	}
}
""")

def parse(fname):
	compiler = BooCompiler()
	compiler.Parameters.Pipeline = Parse()
	compiler.Parameters.Input.Add(Boo.Lang.Compiler.IO.FileInput(fname))
	result = compiler.Run()
	raise join(result.Errors, "\n") if len(result.Errors)
	return result.CompileUnit

start = date.Now

module = parse("ast.model.boo").Modules[0]

WriteVisitor(module)
WriteDepthFirstVisitor(module)
WriteDepthFirstTransformer(module)
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
