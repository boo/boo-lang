﻿#region license
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

//
// DO NOT EDIT THIS FILE!
//
// This file was generated automatically by
// astgenerator.boo on 5/11/2004 10:19:16 AM
//

namespace Boo.Lang.Compiler.Ast
{
	using System;
	
	public class DepthFirstVisitor : IAstVisitor
	{
		public bool Accept(Node node)
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
					throw Boo.Lang.Compiler.CompilerErrorFactory.InternalError(node, error);
				}
			}
			return false;
		}
		
		public void Accept(Node[] array, NodeType nodeType)
		{
			foreach (Node node in array)
			{
				if (node.NodeType == nodeType)
				{
					Accept(node);
				}
			}
		}
		
		public bool Accept(NodeCollection collection, NodeType nodeType)
		{
			if (null != collection)
			{
				Accept(collection.ToArray(), nodeType);
				return true;
			}
			return false;
		}
		
		public void Accept(Node[] array)
		{
			foreach (Node node in array)
			{
				Accept(node);
			}
		}
		
		public bool Accept(NodeCollection collection)
		{
			if (null != collection)
			{
				Accept(collection.ToArray());
				return true;
			}
			return false;
		}
		
		public virtual void OnCompileUnit(Boo.Lang.Compiler.Ast.CompileUnit node)
		{				
			if (EnterCompileUnit(node))
			{
				Accept(node.Modules);
				LeaveCompileUnit(node);
			}
		}
			
		public virtual bool EnterCompileUnit(Boo.Lang.Compiler.Ast.CompileUnit node)
		{
			return true;
		}
		
		public virtual void LeaveCompileUnit(Boo.Lang.Compiler.Ast.CompileUnit node)
		{
		}
			
		public virtual void OnSimpleTypeReference(Boo.Lang.Compiler.Ast.SimpleTypeReference node)
		{
		}
			
		public virtual void OnArrayTypeReference(Boo.Lang.Compiler.Ast.ArrayTypeReference node)
		{				
			if (EnterArrayTypeReference(node))
			{
				Accept(node.ElementType);
				LeaveArrayTypeReference(node);
			}
		}
			
		public virtual bool EnterArrayTypeReference(Boo.Lang.Compiler.Ast.ArrayTypeReference node)
		{
			return true;
		}
		
		public virtual void LeaveArrayTypeReference(Boo.Lang.Compiler.Ast.ArrayTypeReference node)
		{
		}
			
		public virtual void OnNamespaceDeclaration(Boo.Lang.Compiler.Ast.NamespaceDeclaration node)
		{
		}
			
		public virtual void OnImport(Boo.Lang.Compiler.Ast.Import node)
		{				
			if (EnterImport(node))
			{
				Accept(node.AssemblyReference);
				Accept(node.Alias);
				LeaveImport(node);
			}
		}
			
		public virtual bool EnterImport(Boo.Lang.Compiler.Ast.Import node)
		{
			return true;
		}
		
		public virtual void LeaveImport(Boo.Lang.Compiler.Ast.Import node)
		{
		}
			
		public virtual void OnModule(Boo.Lang.Compiler.Ast.Module node)
		{				
			if (EnterModule(node))
			{
				Accept(node.Attributes);
				Accept(node.Members);
				Accept(node.BaseTypes);
				Accept(node.Namespace);
				Accept(node.Imports);
				Accept(node.Globals);
				LeaveModule(node);
			}
		}
			
		public virtual bool EnterModule(Boo.Lang.Compiler.Ast.Module node)
		{
			return true;
		}
		
		public virtual void LeaveModule(Boo.Lang.Compiler.Ast.Module node)
		{
		}
			
		public virtual void OnClassDefinition(Boo.Lang.Compiler.Ast.ClassDefinition node)
		{				
			if (EnterClassDefinition(node))
			{
				Accept(node.Attributes);
				Accept(node.Members);
				Accept(node.BaseTypes);
				LeaveClassDefinition(node);
			}
		}
			
		public virtual bool EnterClassDefinition(Boo.Lang.Compiler.Ast.ClassDefinition node)
		{
			return true;
		}
		
		public virtual void LeaveClassDefinition(Boo.Lang.Compiler.Ast.ClassDefinition node)
		{
		}
			
		public virtual void OnInterfaceDefinition(Boo.Lang.Compiler.Ast.InterfaceDefinition node)
		{				
			if (EnterInterfaceDefinition(node))
			{
				Accept(node.Attributes);
				Accept(node.Members);
				Accept(node.BaseTypes);
				LeaveInterfaceDefinition(node);
			}
		}
			
		public virtual bool EnterInterfaceDefinition(Boo.Lang.Compiler.Ast.InterfaceDefinition node)
		{
			return true;
		}
		
		public virtual void LeaveInterfaceDefinition(Boo.Lang.Compiler.Ast.InterfaceDefinition node)
		{
		}
			
		public virtual void OnEnumDefinition(Boo.Lang.Compiler.Ast.EnumDefinition node)
		{				
			if (EnterEnumDefinition(node))
			{
				Accept(node.Attributes);
				Accept(node.Members);
				Accept(node.BaseTypes);
				LeaveEnumDefinition(node);
			}
		}
			
		public virtual bool EnterEnumDefinition(Boo.Lang.Compiler.Ast.EnumDefinition node)
		{
			return true;
		}
		
		public virtual void LeaveEnumDefinition(Boo.Lang.Compiler.Ast.EnumDefinition node)
		{
		}
			
		public virtual void OnEnumMember(Boo.Lang.Compiler.Ast.EnumMember node)
		{				
			if (EnterEnumMember(node))
			{
				Accept(node.Attributes);
				Accept(node.Initializer);
				LeaveEnumMember(node);
			}
		}
			
		public virtual bool EnterEnumMember(Boo.Lang.Compiler.Ast.EnumMember node)
		{
			return true;
		}
		
		public virtual void LeaveEnumMember(Boo.Lang.Compiler.Ast.EnumMember node)
		{
		}
			
		public virtual void OnField(Boo.Lang.Compiler.Ast.Field node)
		{				
			if (EnterField(node))
			{
				Accept(node.Attributes);
				Accept(node.Type);
				Accept(node.Initializer);
				LeaveField(node);
			}
		}
			
		public virtual bool EnterField(Boo.Lang.Compiler.Ast.Field node)
		{
			return true;
		}
		
		public virtual void LeaveField(Boo.Lang.Compiler.Ast.Field node)
		{
		}
			
		public virtual void OnProperty(Boo.Lang.Compiler.Ast.Property node)
		{				
			if (EnterProperty(node))
			{
				Accept(node.Attributes);
				Accept(node.Parameters);
				Accept(node.Getter);
				Accept(node.Setter);
				Accept(node.Type);
				LeaveProperty(node);
			}
		}
			
		public virtual bool EnterProperty(Boo.Lang.Compiler.Ast.Property node)
		{
			return true;
		}
		
		public virtual void LeaveProperty(Boo.Lang.Compiler.Ast.Property node)
		{
		}
			
		public virtual void OnLocal(Boo.Lang.Compiler.Ast.Local node)
		{
		}
			
		public virtual void OnCallableBlockExpression(Boo.Lang.Compiler.Ast.CallableBlockExpression node)
		{				
			if (EnterCallableBlockExpression(node))
			{
				Accept(node.Parameters);
				Accept(node.ReturnType);
				Accept(node.Body);
				LeaveCallableBlockExpression(node);
			}
		}
			
		public virtual bool EnterCallableBlockExpression(Boo.Lang.Compiler.Ast.CallableBlockExpression node)
		{
			return true;
		}
		
		public virtual void LeaveCallableBlockExpression(Boo.Lang.Compiler.Ast.CallableBlockExpression node)
		{
		}
			
		public virtual void OnMethod(Boo.Lang.Compiler.Ast.Method node)
		{				
			if (EnterMethod(node))
			{
				Accept(node.Attributes);
				Accept(node.Parameters);
				Accept(node.ReturnType);
				Accept(node.ReturnTypeAttributes);
				Accept(node.Body);
				Accept(node.Locals);
				LeaveMethod(node);
			}
		}
			
		public virtual bool EnterMethod(Boo.Lang.Compiler.Ast.Method node)
		{
			return true;
		}
		
		public virtual void LeaveMethod(Boo.Lang.Compiler.Ast.Method node)
		{
		}
			
		public virtual void OnConstructor(Boo.Lang.Compiler.Ast.Constructor node)
		{				
			if (EnterConstructor(node))
			{
				Accept(node.Attributes);
				Accept(node.Parameters);
				Accept(node.ReturnType);
				Accept(node.ReturnTypeAttributes);
				Accept(node.Body);
				Accept(node.Locals);
				LeaveConstructor(node);
			}
		}
			
		public virtual bool EnterConstructor(Boo.Lang.Compiler.Ast.Constructor node)
		{
			return true;
		}
		
		public virtual void LeaveConstructor(Boo.Lang.Compiler.Ast.Constructor node)
		{
		}
			
		public virtual void OnParameterDeclaration(Boo.Lang.Compiler.Ast.ParameterDeclaration node)
		{				
			if (EnterParameterDeclaration(node))
			{
				Accept(node.Type);
				Accept(node.Attributes);
				LeaveParameterDeclaration(node);
			}
		}
			
		public virtual bool EnterParameterDeclaration(Boo.Lang.Compiler.Ast.ParameterDeclaration node)
		{
			return true;
		}
		
		public virtual void LeaveParameterDeclaration(Boo.Lang.Compiler.Ast.ParameterDeclaration node)
		{
		}
			
		public virtual void OnDeclaration(Boo.Lang.Compiler.Ast.Declaration node)
		{				
			if (EnterDeclaration(node))
			{
				Accept(node.Type);
				LeaveDeclaration(node);
			}
		}
			
		public virtual bool EnterDeclaration(Boo.Lang.Compiler.Ast.Declaration node)
		{
			return true;
		}
		
		public virtual void LeaveDeclaration(Boo.Lang.Compiler.Ast.Declaration node)
		{
		}
			
		public virtual void OnAttribute(Boo.Lang.Compiler.Ast.Attribute node)
		{				
			if (EnterAttribute(node))
			{
				Accept(node.Arguments);
				Accept(node.NamedArguments);
				LeaveAttribute(node);
			}
		}
			
		public virtual bool EnterAttribute(Boo.Lang.Compiler.Ast.Attribute node)
		{
			return true;
		}
		
		public virtual void LeaveAttribute(Boo.Lang.Compiler.Ast.Attribute node)
		{
		}
			
		public virtual void OnStatementModifier(Boo.Lang.Compiler.Ast.StatementModifier node)
		{				
			if (EnterStatementModifier(node))
			{
				Accept(node.Condition);
				LeaveStatementModifier(node);
			}
		}
			
		public virtual bool EnterStatementModifier(Boo.Lang.Compiler.Ast.StatementModifier node)
		{
			return true;
		}
		
		public virtual void LeaveStatementModifier(Boo.Lang.Compiler.Ast.StatementModifier node)
		{
		}
			
		public virtual void OnBlock(Boo.Lang.Compiler.Ast.Block node)
		{				
			if (EnterBlock(node))
			{
				Accept(node.Modifier);
				Accept(node.Statements);
				LeaveBlock(node);
			}
		}
			
		public virtual bool EnterBlock(Boo.Lang.Compiler.Ast.Block node)
		{
			return true;
		}
		
		public virtual void LeaveBlock(Boo.Lang.Compiler.Ast.Block node)
		{
		}
			
		public virtual void OnDeclarationStatement(Boo.Lang.Compiler.Ast.DeclarationStatement node)
		{				
			if (EnterDeclarationStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Declaration);
				Accept(node.Initializer);
				LeaveDeclarationStatement(node);
			}
		}
			
		public virtual bool EnterDeclarationStatement(Boo.Lang.Compiler.Ast.DeclarationStatement node)
		{
			return true;
		}
		
		public virtual void LeaveDeclarationStatement(Boo.Lang.Compiler.Ast.DeclarationStatement node)
		{
		}
			
		public virtual void OnAssertStatement(Boo.Lang.Compiler.Ast.AssertStatement node)
		{				
			if (EnterAssertStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Condition);
				Accept(node.Message);
				LeaveAssertStatement(node);
			}
		}
			
		public virtual bool EnterAssertStatement(Boo.Lang.Compiler.Ast.AssertStatement node)
		{
			return true;
		}
		
		public virtual void LeaveAssertStatement(Boo.Lang.Compiler.Ast.AssertStatement node)
		{
		}
			
		public virtual void OnMacroStatement(Boo.Lang.Compiler.Ast.MacroStatement node)
		{				
			if (EnterMacroStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Arguments);
				Accept(node.Block);
				LeaveMacroStatement(node);
			}
		}
			
		public virtual bool EnterMacroStatement(Boo.Lang.Compiler.Ast.MacroStatement node)
		{
			return true;
		}
		
		public virtual void LeaveMacroStatement(Boo.Lang.Compiler.Ast.MacroStatement node)
		{
		}
			
		public virtual void OnTryStatement(Boo.Lang.Compiler.Ast.TryStatement node)
		{				
			if (EnterTryStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.ProtectedBlock);
				Accept(node.ExceptionHandlers);
				Accept(node.SuccessBlock);
				Accept(node.EnsureBlock);
				LeaveTryStatement(node);
			}
		}
			
		public virtual bool EnterTryStatement(Boo.Lang.Compiler.Ast.TryStatement node)
		{
			return true;
		}
		
		public virtual void LeaveTryStatement(Boo.Lang.Compiler.Ast.TryStatement node)
		{
		}
			
		public virtual void OnExceptionHandler(Boo.Lang.Compiler.Ast.ExceptionHandler node)
		{				
			if (EnterExceptionHandler(node))
			{
				Accept(node.Declaration);
				Accept(node.Block);
				LeaveExceptionHandler(node);
			}
		}
			
		public virtual bool EnterExceptionHandler(Boo.Lang.Compiler.Ast.ExceptionHandler node)
		{
			return true;
		}
		
		public virtual void LeaveExceptionHandler(Boo.Lang.Compiler.Ast.ExceptionHandler node)
		{
		}
			
		public virtual void OnIfStatement(Boo.Lang.Compiler.Ast.IfStatement node)
		{				
			if (EnterIfStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Condition);
				Accept(node.TrueBlock);
				Accept(node.FalseBlock);
				LeaveIfStatement(node);
			}
		}
			
		public virtual bool EnterIfStatement(Boo.Lang.Compiler.Ast.IfStatement node)
		{
			return true;
		}
		
		public virtual void LeaveIfStatement(Boo.Lang.Compiler.Ast.IfStatement node)
		{
		}
			
		public virtual void OnUnlessStatement(Boo.Lang.Compiler.Ast.UnlessStatement node)
		{				
			if (EnterUnlessStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Condition);
				Accept(node.Block);
				LeaveUnlessStatement(node);
			}
		}
			
		public virtual bool EnterUnlessStatement(Boo.Lang.Compiler.Ast.UnlessStatement node)
		{
			return true;
		}
		
		public virtual void LeaveUnlessStatement(Boo.Lang.Compiler.Ast.UnlessStatement node)
		{
		}
			
		public virtual void OnForStatement(Boo.Lang.Compiler.Ast.ForStatement node)
		{				
			if (EnterForStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Declarations);
				Accept(node.Iterator);
				Accept(node.Block);
				LeaveForStatement(node);
			}
		}
			
		public virtual bool EnterForStatement(Boo.Lang.Compiler.Ast.ForStatement node)
		{
			return true;
		}
		
		public virtual void LeaveForStatement(Boo.Lang.Compiler.Ast.ForStatement node)
		{
		}
			
		public virtual void OnWhileStatement(Boo.Lang.Compiler.Ast.WhileStatement node)
		{				
			if (EnterWhileStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Condition);
				Accept(node.Block);
				LeaveWhileStatement(node);
			}
		}
			
		public virtual bool EnterWhileStatement(Boo.Lang.Compiler.Ast.WhileStatement node)
		{
			return true;
		}
		
		public virtual void LeaveWhileStatement(Boo.Lang.Compiler.Ast.WhileStatement node)
		{
		}
			
		public virtual void OnGivenStatement(Boo.Lang.Compiler.Ast.GivenStatement node)
		{				
			if (EnterGivenStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Expression);
				Accept(node.WhenClauses);
				Accept(node.OtherwiseBlock);
				LeaveGivenStatement(node);
			}
		}
			
		public virtual bool EnterGivenStatement(Boo.Lang.Compiler.Ast.GivenStatement node)
		{
			return true;
		}
		
		public virtual void LeaveGivenStatement(Boo.Lang.Compiler.Ast.GivenStatement node)
		{
		}
			
		public virtual void OnWhenClause(Boo.Lang.Compiler.Ast.WhenClause node)
		{				
			if (EnterWhenClause(node))
			{
				Accept(node.Condition);
				Accept(node.Block);
				LeaveWhenClause(node);
			}
		}
			
		public virtual bool EnterWhenClause(Boo.Lang.Compiler.Ast.WhenClause node)
		{
			return true;
		}
		
		public virtual void LeaveWhenClause(Boo.Lang.Compiler.Ast.WhenClause node)
		{
		}
			
		public virtual void OnBreakStatement(Boo.Lang.Compiler.Ast.BreakStatement node)
		{				
			if (EnterBreakStatement(node))
			{
				Accept(node.Modifier);
				LeaveBreakStatement(node);
			}
		}
			
		public virtual bool EnterBreakStatement(Boo.Lang.Compiler.Ast.BreakStatement node)
		{
			return true;
		}
		
		public virtual void LeaveBreakStatement(Boo.Lang.Compiler.Ast.BreakStatement node)
		{
		}
			
		public virtual void OnContinueStatement(Boo.Lang.Compiler.Ast.ContinueStatement node)
		{				
			if (EnterContinueStatement(node))
			{
				Accept(node.Modifier);
				LeaveContinueStatement(node);
			}
		}
			
		public virtual bool EnterContinueStatement(Boo.Lang.Compiler.Ast.ContinueStatement node)
		{
			return true;
		}
		
		public virtual void LeaveContinueStatement(Boo.Lang.Compiler.Ast.ContinueStatement node)
		{
		}
			
		public virtual void OnRetryStatement(Boo.Lang.Compiler.Ast.RetryStatement node)
		{				
			if (EnterRetryStatement(node))
			{
				Accept(node.Modifier);
				LeaveRetryStatement(node);
			}
		}
			
		public virtual bool EnterRetryStatement(Boo.Lang.Compiler.Ast.RetryStatement node)
		{
			return true;
		}
		
		public virtual void LeaveRetryStatement(Boo.Lang.Compiler.Ast.RetryStatement node)
		{
		}
			
		public virtual void OnReturnStatement(Boo.Lang.Compiler.Ast.ReturnStatement node)
		{				
			if (EnterReturnStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Expression);
				LeaveReturnStatement(node);
			}
		}
			
		public virtual bool EnterReturnStatement(Boo.Lang.Compiler.Ast.ReturnStatement node)
		{
			return true;
		}
		
		public virtual void LeaveReturnStatement(Boo.Lang.Compiler.Ast.ReturnStatement node)
		{
		}
			
		public virtual void OnYieldStatement(Boo.Lang.Compiler.Ast.YieldStatement node)
		{				
			if (EnterYieldStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Expression);
				LeaveYieldStatement(node);
			}
		}
			
		public virtual bool EnterYieldStatement(Boo.Lang.Compiler.Ast.YieldStatement node)
		{
			return true;
		}
		
		public virtual void LeaveYieldStatement(Boo.Lang.Compiler.Ast.YieldStatement node)
		{
		}
			
		public virtual void OnRaiseStatement(Boo.Lang.Compiler.Ast.RaiseStatement node)
		{				
			if (EnterRaiseStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Exception);
				LeaveRaiseStatement(node);
			}
		}
			
		public virtual bool EnterRaiseStatement(Boo.Lang.Compiler.Ast.RaiseStatement node)
		{
			return true;
		}
		
		public virtual void LeaveRaiseStatement(Boo.Lang.Compiler.Ast.RaiseStatement node)
		{
		}
			
		public virtual void OnUnpackStatement(Boo.Lang.Compiler.Ast.UnpackStatement node)
		{				
			if (EnterUnpackStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Declarations);
				Accept(node.Expression);
				LeaveUnpackStatement(node);
			}
		}
			
		public virtual bool EnterUnpackStatement(Boo.Lang.Compiler.Ast.UnpackStatement node)
		{
			return true;
		}
		
		public virtual void LeaveUnpackStatement(Boo.Lang.Compiler.Ast.UnpackStatement node)
		{
		}
			
		public virtual void OnExpressionStatement(Boo.Lang.Compiler.Ast.ExpressionStatement node)
		{				
			if (EnterExpressionStatement(node))
			{
				Accept(node.Modifier);
				Accept(node.Expression);
				LeaveExpressionStatement(node);
			}
		}
			
		public virtual bool EnterExpressionStatement(Boo.Lang.Compiler.Ast.ExpressionStatement node)
		{
			return true;
		}
		
		public virtual void LeaveExpressionStatement(Boo.Lang.Compiler.Ast.ExpressionStatement node)
		{
		}
			
		public virtual void OnOmittedExpression(Boo.Lang.Compiler.Ast.OmittedExpression node)
		{
		}
			
		public virtual void OnExpressionPair(Boo.Lang.Compiler.Ast.ExpressionPair node)
		{				
			if (EnterExpressionPair(node))
			{
				Accept(node.First);
				Accept(node.Second);
				LeaveExpressionPair(node);
			}
		}
			
		public virtual bool EnterExpressionPair(Boo.Lang.Compiler.Ast.ExpressionPair node)
		{
			return true;
		}
		
		public virtual void LeaveExpressionPair(Boo.Lang.Compiler.Ast.ExpressionPair node)
		{
		}
			
		public virtual void OnMethodInvocationExpression(Boo.Lang.Compiler.Ast.MethodInvocationExpression node)
		{				
			if (EnterMethodInvocationExpression(node))
			{
				Accept(node.Target);
				Accept(node.Arguments);
				Accept(node.NamedArguments);
				LeaveMethodInvocationExpression(node);
			}
		}
			
		public virtual bool EnterMethodInvocationExpression(Boo.Lang.Compiler.Ast.MethodInvocationExpression node)
		{
			return true;
		}
		
		public virtual void LeaveMethodInvocationExpression(Boo.Lang.Compiler.Ast.MethodInvocationExpression node)
		{
		}
			
		public virtual void OnUnaryExpression(Boo.Lang.Compiler.Ast.UnaryExpression node)
		{				
			if (EnterUnaryExpression(node))
			{
				Accept(node.Operand);
				LeaveUnaryExpression(node);
			}
		}
			
		public virtual bool EnterUnaryExpression(Boo.Lang.Compiler.Ast.UnaryExpression node)
		{
			return true;
		}
		
		public virtual void LeaveUnaryExpression(Boo.Lang.Compiler.Ast.UnaryExpression node)
		{
		}
			
		public virtual void OnBinaryExpression(Boo.Lang.Compiler.Ast.BinaryExpression node)
		{				
			if (EnterBinaryExpression(node))
			{
				Accept(node.Left);
				Accept(node.Right);
				LeaveBinaryExpression(node);
			}
		}
			
		public virtual bool EnterBinaryExpression(Boo.Lang.Compiler.Ast.BinaryExpression node)
		{
			return true;
		}
		
		public virtual void LeaveBinaryExpression(Boo.Lang.Compiler.Ast.BinaryExpression node)
		{
		}
			
		public virtual void OnReferenceExpression(Boo.Lang.Compiler.Ast.ReferenceExpression node)
		{
		}
			
		public virtual void OnMemberReferenceExpression(Boo.Lang.Compiler.Ast.MemberReferenceExpression node)
		{				
			if (EnterMemberReferenceExpression(node))
			{
				Accept(node.Target);
				LeaveMemberReferenceExpression(node);
			}
		}
			
		public virtual bool EnterMemberReferenceExpression(Boo.Lang.Compiler.Ast.MemberReferenceExpression node)
		{
			return true;
		}
		
		public virtual void LeaveMemberReferenceExpression(Boo.Lang.Compiler.Ast.MemberReferenceExpression node)
		{
		}
			
		public virtual void OnStringLiteralExpression(Boo.Lang.Compiler.Ast.StringLiteralExpression node)
		{
		}
			
		public virtual void OnTimeSpanLiteralExpression(Boo.Lang.Compiler.Ast.TimeSpanLiteralExpression node)
		{
		}
			
		public virtual void OnIntegerLiteralExpression(Boo.Lang.Compiler.Ast.IntegerLiteralExpression node)
		{
		}
			
		public virtual void OnDoubleLiteralExpression(Boo.Lang.Compiler.Ast.DoubleLiteralExpression node)
		{
		}
			
		public virtual void OnNullLiteralExpression(Boo.Lang.Compiler.Ast.NullLiteralExpression node)
		{
		}
			
		public virtual void OnSelfLiteralExpression(Boo.Lang.Compiler.Ast.SelfLiteralExpression node)
		{
		}
			
		public virtual void OnSuperLiteralExpression(Boo.Lang.Compiler.Ast.SuperLiteralExpression node)
		{
		}
			
		public virtual void OnBoolLiteralExpression(Boo.Lang.Compiler.Ast.BoolLiteralExpression node)
		{
		}
			
		public virtual void OnRELiteralExpression(Boo.Lang.Compiler.Ast.RELiteralExpression node)
		{
		}
			
		public virtual void OnExpressionInterpolationExpression(Boo.Lang.Compiler.Ast.ExpressionInterpolationExpression node)
		{				
			if (EnterExpressionInterpolationExpression(node))
			{
				Accept(node.Expressions);
				LeaveExpressionInterpolationExpression(node);
			}
		}
			
		public virtual bool EnterExpressionInterpolationExpression(Boo.Lang.Compiler.Ast.ExpressionInterpolationExpression node)
		{
			return true;
		}
		
		public virtual void LeaveExpressionInterpolationExpression(Boo.Lang.Compiler.Ast.ExpressionInterpolationExpression node)
		{
		}
			
		public virtual void OnHashLiteralExpression(Boo.Lang.Compiler.Ast.HashLiteralExpression node)
		{				
			if (EnterHashLiteralExpression(node))
			{
				Accept(node.Items);
				LeaveHashLiteralExpression(node);
			}
		}
			
		public virtual bool EnterHashLiteralExpression(Boo.Lang.Compiler.Ast.HashLiteralExpression node)
		{
			return true;
		}
		
		public virtual void LeaveHashLiteralExpression(Boo.Lang.Compiler.Ast.HashLiteralExpression node)
		{
		}
			
		public virtual void OnListLiteralExpression(Boo.Lang.Compiler.Ast.ListLiteralExpression node)
		{				
			if (EnterListLiteralExpression(node))
			{
				Accept(node.Items);
				LeaveListLiteralExpression(node);
			}
		}
			
		public virtual bool EnterListLiteralExpression(Boo.Lang.Compiler.Ast.ListLiteralExpression node)
		{
			return true;
		}
		
		public virtual void LeaveListLiteralExpression(Boo.Lang.Compiler.Ast.ListLiteralExpression node)
		{
		}
			
		public virtual void OnArrayLiteralExpression(Boo.Lang.Compiler.Ast.ArrayLiteralExpression node)
		{				
			if (EnterArrayLiteralExpression(node))
			{
				Accept(node.Items);
				LeaveArrayLiteralExpression(node);
			}
		}
			
		public virtual bool EnterArrayLiteralExpression(Boo.Lang.Compiler.Ast.ArrayLiteralExpression node)
		{
			return true;
		}
		
		public virtual void LeaveArrayLiteralExpression(Boo.Lang.Compiler.Ast.ArrayLiteralExpression node)
		{
		}
			
		public virtual void OnGeneratorExpression(Boo.Lang.Compiler.Ast.GeneratorExpression node)
		{				
			if (EnterGeneratorExpression(node))
			{
				Accept(node.Expression);
				Accept(node.Declarations);
				Accept(node.Iterator);
				Accept(node.Filter);
				LeaveGeneratorExpression(node);
			}
		}
			
		public virtual bool EnterGeneratorExpression(Boo.Lang.Compiler.Ast.GeneratorExpression node)
		{
			return true;
		}
		
		public virtual void LeaveGeneratorExpression(Boo.Lang.Compiler.Ast.GeneratorExpression node)
		{
		}
			
		public virtual void OnSlicingExpression(Boo.Lang.Compiler.Ast.SlicingExpression node)
		{				
			if (EnterSlicingExpression(node))
			{
				Accept(node.Target);
				Accept(node.Begin);
				Accept(node.End);
				Accept(node.Step);
				LeaveSlicingExpression(node);
			}
		}
			
		public virtual bool EnterSlicingExpression(Boo.Lang.Compiler.Ast.SlicingExpression node)
		{
			return true;
		}
		
		public virtual void LeaveSlicingExpression(Boo.Lang.Compiler.Ast.SlicingExpression node)
		{
		}
			
		public virtual void OnAsExpression(Boo.Lang.Compiler.Ast.AsExpression node)
		{				
			if (EnterAsExpression(node))
			{
				Accept(node.Target);
				Accept(node.Type);
				LeaveAsExpression(node);
			}
		}
			
		public virtual bool EnterAsExpression(Boo.Lang.Compiler.Ast.AsExpression node)
		{
			return true;
		}
		
		public virtual void LeaveAsExpression(Boo.Lang.Compiler.Ast.AsExpression node)
		{
		}
			
		public virtual void OnCastExpression(Boo.Lang.Compiler.Ast.CastExpression node)
		{				
			if (EnterCastExpression(node))
			{
				Accept(node.Type);
				Accept(node.Target);
				LeaveCastExpression(node);
			}
		}
			
		public virtual bool EnterCastExpression(Boo.Lang.Compiler.Ast.CastExpression node)
		{
			return true;
		}
		
		public virtual void LeaveCastExpression(Boo.Lang.Compiler.Ast.CastExpression node)
		{
		}
			
		public virtual void OnTypeofExpression(Boo.Lang.Compiler.Ast.TypeofExpression node)
		{				
			if (EnterTypeofExpression(node))
			{
				Accept(node.Type);
				LeaveTypeofExpression(node);
			}
		}
			
		public virtual bool EnterTypeofExpression(Boo.Lang.Compiler.Ast.TypeofExpression node)
		{
			return true;
		}
		
		public virtual void LeaveTypeofExpression(Boo.Lang.Compiler.Ast.TypeofExpression node)
		{
		}
			
	}
}
