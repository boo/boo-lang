#region license
// Copyright (c) 2004, Daniel Grunwald (daniel@danielgrunwald.de)
// All rights reserved.
//
// BooBinding is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// BooBinding is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with BooBinding; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion

namespace BooBinding

import System
import System.Collections
import System.Text
import SharpDevelop.Internal.Parser
import ICSharpCode.SharpDevelop.Services
import ICSharpCode.Core.Properties
import ICSharpCode.Core.Services

class BooAmbience(AbstractAmbience):
	[Getter(TypeConversionTable)]
	static _typeConversionTable = {
		'System.Void'    : 'void',
		'System.Object'  : 'object',
		'System.Boolean' : 'bool',
		'System.Byte'    : 'byte',
		'System.SByte'   : 'sbyte',
		'System.Char'   : 'char',
		//'System.Enum'   : 'enum',
		'System.Int16'  : 'short',
		'System.Int32'  : 'int',
		'System.Int64'  : 'long',
		'System.UInt16' : 'ushort',
		'System.UInt32' : 'uint',
		'System.UInt64' : 'ulong',
		'System.Single' : 'single',
		'System.Double' : 'double',
		'System.Decimal'  : 'decimal',
		'System.String'   : 'string',
		'System.DateTime' : 'date',
		'System.TimeSpan' : 'timespan',
		'System.Type'  : 'type',
		'System.Array' : 'array',
		'System.Text.RegularExpressions.Regex' : 'regex'
		}
	
	static _reverseTypeConversionTable as Hashtable
	
	static ReverseTypeConversionTable:
		get:
			if _reverseTypeConversionTable == null:
				_reverseTypeConversionTable = Hashtable()
				for e as DictionaryEntry in _typeConversionTable:
					_reverseTypeConversionTable.Add(e.Value, e.Key)
			return _reverseTypeConversionTable
	
	
	private def ModifierIsSet(modifier as ModifierEnum, query as ModifierEnum) as bool:
		return (modifier & query) == query
	
	override def Convert(modifier as ModifierEnum) as string:
		if ShowAccessibility:
			if ModifierIsSet(modifier, ModifierEnum.Public):
				return 'public '
			elif ModifierIsSet(modifier, ModifierEnum.Private):
				return 'private '
			elif ModifierIsSet(modifier, ModifierEnum.ProtectedAndInternal):
				return 'protected internal '
			elif ModifierIsSet(modifier, ModifierEnum.ProtectedOrInternal):
				return 'internal protected '
			elif ModifierIsSet(modifier, ModifierEnum.Internal):
				return 'internal '
			elif ModifierIsSet(modifier, ModifierEnum.Protected):
				return 'protected '
		return ''
	
	private def GetModifier(decoration as IDecoration) as string:
		ret as string = ''
		if IncludeHTMLMarkup:
			ret += '<i>'
		
		if decoration.IsStatic:
			ret += 'static '
		elif decoration.IsFinal:
			ret += 'final '
		elif decoration.IsVirtual:
			ret += 'virtual '
		elif decoration.IsOverride:
			ret += 'override '
		elif decoration.IsNew:
			ret += 'new '
		
		if IncludeHTMLMarkup:
			ret += '</i>'
		
		return ret
	
	override def Convert(c as IClass) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(c.Modifiers))
		if IncludeHTMLMarkup:
			builder.Append('<i>')
		
		cType = c.ClassType
		
		if ShowModifiers:
			if c.IsSealed:
				if cType == ClassType.Delegate or cType == ClassType.Enum:
					pass
				else:
					builder.Append('final ')
			elif c.IsAbstract and cType != ClassType.Interface:
				builder.Append('abstract ')
		
		if IncludeHTMLMarkup:
			builder.Append('</i>')
		
		if ShowModifiers:
			if cType == ClassType.Delegate:
				builder.Append('callable ')
			elif cType == ClassType.Class:
				builder.Append('class ')
			elif cType == ClassType.Struct:
				builder.Append('struct ')
			elif cType == ClassType.Interface:
				builder.Append('interface ')
			elif cType == ClassType.Enum:
				builder.Append('enum ')
		
		if cType == ClassType.Delegate and c.Methods.Count > 0:
			for m as IMethod in c.Methods:
				if m.Name == 'Invoke':
					builder.Append(Convert(m.ReturnType))
					builder.Append(' ')
		
		if IncludeHTMLMarkup:
			builder.Append('<b>')
		
		if UseFullyQualifiedMemberNames:
			builder.Append(c.FullyQualifiedName)
		else:
			builder.Append(c.Name)
		
		if IncludeHTMLMarkup:
			builder.Append('</b>')
		
		if c.ClassType == ClassType.Delegate:
			builder.Append(' (')
			if IncludeHTMLMarkup:
				builder.Append('<br>')
			
			for m as IMethod in c.Methods:
				if m.Name == 'Invoke':
					for i in range(m.Parameters.Count):
						if IncludeHTMLMarkup:
							builder.Append('&nbsp;&nbsp;&nbsp;')
						
						builder.Append(Convert(m.Parameters[i]))
						if i + 1 < m.Parameters.Count:
							builder.Append(', ')
						
						if IncludeHTMLMarkup:
							builder.Append('<br>')
			
			builder.Append(Char.Parse(')'))
		elif ShowInheritanceList:
			if c.BaseTypes.Count > 0:
				builder.Append('(')
				for i in range(c.BaseTypes.Count):
					builder.Append(c.BaseTypes[i])
					if i + 1 < c.BaseTypes.Count:
						builder.Append(', ')
				builder.Append(')')
		
		if IncludeBodies:
			builder.Append(':\n')
		
		return builder.ToString()
	
	override def ConvertEnd(c as IClass) as string:
		return ''
	
	override def Convert(field as IField) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(field.Modifiers))
		if IncludeHTMLMarkup:
			builder.Append('<i>')
		
		if ShowModifiers:
			if field.IsStatic and field.IsLiteral:
				builder.Append('const ')
			elif field.IsStatic:
				builder.Append('static ')
			
			if field.IsReadonly:
				builder.Append('readonly ')
			
		
		if IncludeHTMLMarkup:
			builder.Append('</i>')
		
		if IncludeHTMLMarkup:
			builder.Append('<b>')
		
		if UseFullyQualifiedMemberNames:
			builder.Append(field.FullyQualifiedName)
		else:
			builder.Append(field.Name)
		
		if field.ReturnType != null and ShowReturnType:
			builder.Append(' as ')
			builder.Append(Convert(field.ReturnType))
		
		if IncludeHTMLMarkup:
			builder.Append('</b>')
		
		return builder.ToString()
	
	override def Convert(property as IProperty) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(property.Modifiers))
		if ShowModifiers:
			builder.Append(GetModifier(property))
		
		if IncludeHTMLMarkup:
			builder.Append('<b>')
		
		if UseFullyQualifiedMemberNames:
			builder.Append(property.FullyQualifiedName)
		else:
			builder.Append(property.Name)
		
		if IncludeHTMLMarkup:
			builder.Append('</b>')
		
		if property.Parameters.Count > 0:
			builder.Append('(')
			if IncludeHTMLMarkup:
				builder.Append('<br>')
			
			for i in range(property.Parameters.Count):
				if IncludeHTMLMarkup:
					builder.Append('&nbsp;&nbsp;&nbsp;')
				
				builder.Append(Convert(property.Parameters[i]))
				if i + 1 < property.Parameters.Count:
					builder.Append(', ')
				
				if IncludeHTMLMarkup:
					builder.Append('<br>')
			
			builder.Append(')')
		
		if property.ReturnType != null and ShowReturnType:
			builder.Append(' as ')
			builder.Append(Convert(property.ReturnType))
		
		if IncludeBodies:
			builder.Append(': ')
			if property.CanGet:
				builder.Append('get ')
			
			if property.CanSet:
				builder.Append('set ')
		
		return builder.ToString()
	
	override def Convert(e as IEvent) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(e.Modifiers))
		if ShowModifiers:
			builder.Append(GetModifier(e))
		
		if IncludeHTMLMarkup:
			builder.Append('<b>')
		
		if UseFullyQualifiedMemberNames:
			builder.Append(e.FullyQualifiedName)
		else:
			builder.Append(e.Name)
		
		if IncludeHTMLMarkup:
			builder.Append('</b>')
		
		if e.ReturnType != null and ShowReturnType:
			builder.Append(' as ')
			builder.Append(Convert(e.ReturnType))
		
		return builder.ToString()
	
	override def Convert(m as IIndexer) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(m.Modifiers))
		if IncludeHTMLMarkup:
			builder.Append('<i>')
		
		if ShowModifiers and m.IsStatic:
			builder.Append('static ')
		
		if IncludeHTMLMarkup:
			builder.Append('</i>')
		
		if m.ReturnType != null and ShowReturnType:
			builder.Append(Convert(m.ReturnType))
			builder.Append(' ')
		
		if IncludeHTMLMarkup:
			builder.Append('<b>')
		
		if UseFullyQualifiedMemberNames:
			builder.Append(m.FullyQualifiedName)
		else:
			builder.Append(m.Name)
		
		if IncludeHTMLMarkup:
			builder.Append('</b>')
		
		builder.Append('Indexer(')
		if IncludeHTMLMarkup:
			builder.Append('<br>')
		
		for i in range(m.Parameters.Count):
			if IncludeHTMLMarkup:
				builder.Append('&nbsp;&nbsp;&nbsp;')
			
			builder.Append(Convert(m.Parameters[i]))
			if i + 1 < m.Parameters.Count:
				builder.Append(', ')
			
			if IncludeHTMLMarkup:
				builder.Append('<br>')
		
		builder.Append(')')
		
		return builder.ToString()
	
	override def Convert(m as IMethod) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(m.Modifiers))
		if ShowModifiers:
			builder.Append(GetModifier(m))
		
		builder.Append('def ') if ShowReturnType
		
		if IncludeHTMLMarkup:
			builder.Append('<b>')
		
		if m.IsConstructor:
			builder.Append('constructor')
		else:
			if UseFullyQualifiedMemberNames:
				builder.Append(m.FullyQualifiedName)
			else:
				builder.Append(m.Name)
		
		if IncludeHTMLMarkup:
			builder.Append('</b>')
		
		builder.Append('(')
		if IncludeHTMLMarkup:
			builder.Append('<br>')
		
		for i in range(m.Parameters.Count):
			if IncludeHTMLMarkup:
				builder.Append('&nbsp;&nbsp;&nbsp;')
			
			builder.Append(Convert(m.Parameters[i]))
			if i + 1 < m.Parameters.Count:
				builder.Append(', ')
			
			if IncludeHTMLMarkup:
				builder.Append('<br>')
		
		builder.Append(')')
		
		if m.ReturnType != null and ShowReturnType and not m.IsConstructor:
			builder.Append(' as ')
			builder.Append(Convert(m.ReturnType))
		
		if IncludeBodies:
			if m.DeclaringType != null:
				if m.DeclaringType.ClassType != ClassType.Interface:
					builder.Append(': ')
			else:
				builder.Append(': ')
			
		
		return builder.ToString()
	
	override def ConvertEnd(m as IMethod) as string:
		return ''
	
	override def Convert(returnType as IReturnType) as string:
		if returnType == null:
			return ''
		
		builder as StringBuilder = StringBuilder()
		/*
		linkSet as bool = false
		if UseLinkArrayList:
			ret as SharpAssemblyReturnType = returnType as SharpAssemblyReturnType
			if ret != null:
				if ret.UnderlyingClass != null:
					builder.Append('<a href=\'as://' + linkArrayList.Add(ret.UnderlyingClass) + '\'>')
					linkSet = true
		*/
		
		for i in range(returnType.ArrayCount):
			builder.Append('(')
		
		if returnType.FullyQualifiedName != null and _typeConversionTable[returnType.FullyQualifiedName] != null:
			builder.Append(_typeConversionTable[returnType.FullyQualifiedName])
		else:
			if UseFullyQualifiedNames:
				builder.Append(returnType.FullyQualifiedName)
			else:
				builder.Append(returnType.Name)
			
		
		//if linkSet:
		//	builder.Append('</a>')
		
		if returnType.PointerNestingLevel > 0:
			// Sometimes there are negative pointer nesting levels
			// (especially in exception constructors in the BCL
			for i in range(returnType.PointerNestingLevel):
				builder.Append('*')
		
		for i in range(returnType.ArrayCount):
			if returnType.ArrayDimensions[i] > 1:
				builder.Append(',')
				builder.Append(returnType.ArrayDimensions[i])
			builder.Append(')')
		
		return builder.ToString()
	
	override def Convert(param as IParameter) as string:
		builder as StringBuilder = StringBuilder()
		if IncludeHTMLMarkup:
			builder.Append('<i>')
		
		if param.IsRef:
			builder.Append('ref ')
		elif param.IsOut:
			builder.Append('out ')
		elif param.IsParams:
			builder.Append('params ')
		
		if IncludeHTMLMarkup:
			builder.Append('</i>')
		
		if ShowParameterNames:
			builder.Append(param.Name)
			builder.Append(' as ')
		builder.Append(Convert(param.ReturnType))
		
		return builder.ToString()
	
	override def WrapAttribute(attribute as string) as string:
		return '[' + attribute + ']'
	
	override def WrapComment(comment as string) as string:
		return '// ' + comment
	
	override def GetIntrinsicTypeName(dotNetTypeName as string) as string:
		if _typeConversionTable[dotNetTypeName] != null:
			return _typeConversionTable[dotNetTypeName]
		return dotNetTypeName
