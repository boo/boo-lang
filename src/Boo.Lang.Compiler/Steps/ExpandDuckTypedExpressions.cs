﻿#region license
// Copyright (c) 2003, 2004, 2005 Rodrigo B. de Oliveira (rbo@acm.org)
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


using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;

namespace Boo.Lang.Compiler.Steps
{
	/// <summary>
	/// </summary>
	public class ExpandDuckTypedExpressions : AbstractTransformerCompilerStep
	{
		protected IType _runtimeServices;
		protected IMethod RuntimeServices_Invoke;
		protected IMethod RuntimeServices_InvokeCallable;
		protected IMethod RuntimeServices_InvokeBinaryOperator;
		protected IMethod RuntimeServices_InvokeUnaryOperator;
		protected IMethod RuntimeServices_SetProperty;
		protected IMethod RuntimeServices_GetProperty;
		protected IMethod RuntimeServices_GetSlice;
		
		public ExpandDuckTypedExpressions()
		{
		}

		public override void Initialize(CompilerContext context)
		{
			base.Initialize(context);
			_runtimeServices = TypeSystemServices.Map(typeof(Boo.Lang.Runtime.RuntimeServices));
			RuntimeServices_Invoke = ResolveMethod(_runtimeServices, "Invoke");
			RuntimeServices_InvokeCallable = ResolveMethod(_runtimeServices, "InvokeCallable");
			RuntimeServices_InvokeBinaryOperator = ResolveMethod(_runtimeServices, "InvokeBinaryOperator");
			RuntimeServices_InvokeUnaryOperator = ResolveMethod(_runtimeServices, "InvokeUnaryOperator");
			RuntimeServices_SetProperty = ResolveMethod(_runtimeServices, "SetProperty");
			RuntimeServices_GetProperty = ResolveMethod(_runtimeServices, "GetProperty");
			RuntimeServices_GetSlice = ResolveMethod(_runtimeServices, "GetSlice");
		}

		private IMethod ResolveMethod(IType type, string name)
		{
			return NameResolutionService.ResolveMethod(type, name);
		}

		public override void Run()
		{
			if (0 == Errors.Count)
			{
				Visit(CompileUnit);
			}
		}

		override public void OnMethodInvocationExpression(MethodInvocationExpression node)
		{
			if (TypeSystemServices.IsQuackBuiltin(node.Target))
			{
				ExpandQuackInvocation(node);
				return;
			}

			base.OnMethodInvocationExpression(node);
			if (!IsDuckTyped(node.Target)) return;
			
			MethodInvocationExpression invoke = CodeBuilder.CreateMethodInvocation(
				RuntimeServices_InvokeCallable,
				node.Target,
				CodeBuilder.CreateObjectArray(node.Arguments));

			Replace(invoke);
		}

		override public void LeaveSlicingExpression(SlicingExpression node)
		{
			if (!IsDuckTyped(node.Target)) return;

			// todo
			// a[foo]
			// RuntimeServices.GetSlice(a, "", (foo,))
			ArrayLiteralExpression args = new ArrayLiteralExpression();
			foreach (Slice index in node.Indices)
			{
				if (AstUtil.IsComplexSlice(index))
				{
					throw CompilerErrorFactory.NotImplemented(index, "complex slice for duck");
				}
				args.Items.Add(index.Begin);
			}
			BindExpressionType(args, TypeSystemServices.ObjectArrayType);
			
			Expression target = node.Target;
			string memberName = "";
			
			if (NodeType.MemberReferenceExpression == target.NodeType)
			{
				MemberReferenceExpression mre = ((MemberReferenceExpression)target);
				target = mre.Target;
				memberName = mre.Name;
			}
			
			MethodInvocationExpression mie = CodeBuilder.CreateMethodInvocation(
				RuntimeServices_GetSlice,
				target,
				CodeBuilder.CreateStringLiteral(memberName),
				args);
			
			Replace(mie);
		}
		
		override public void LeaveUnaryExpression(UnaryExpression node)
		{
			if (IsDuckTyped(node.Operand) &&
				node.Operator == UnaryOperatorType.UnaryNegation)
			{
				MethodInvocationExpression mie = CodeBuilder.CreateMethodInvocation(
					RuntimeServices_InvokeUnaryOperator,
					CodeBuilder.CreateStringLiteral(
					AstUtil.GetMethodNameForOperator(node.Operator)),
					node.Operand);
				
				Replace(mie);
			}
		}
		
		override public void LeaveBinaryExpression(BinaryExpression node)
		{
			if (BinaryOperatorType.Assign == node.Operator)
			{
				if (TypeSystemServices.IsQuackBuiltin(node.Left))
				{
					ProcessQuackPropertySet(node);
				}
				return;
			}

			if (!AstUtil.IsOverloadableOperator(node.Operator)) return;
			if (!IsDuckTyped(node.Left) && !IsDuckTyped(node.Right)) return;

			MethodInvocationExpression mie = CodeBuilder.CreateMethodInvocation(
				RuntimeServices_InvokeBinaryOperator,
				CodeBuilder.CreateStringLiteral(
				AstUtil.GetMethodNameForOperator(node.Operator)),
				node.Left, node.Right);
			Replace(mie);
		}

		override public void LeaveMemberReferenceExpression(MemberReferenceExpression node)
		{
			if (!TypeSystemServices.IsQuackBuiltin(node)) return;
			
			if (AstUtil.IsLhsOfAssignment(node)
				|| AstUtil.IsTargetOfSlicing(node)) return;

			MethodInvocationExpression mie = CodeBuilder.CreateMethodInvocation(
				RuntimeServices_GetProperty,
				node.Target,
				CodeBuilder.CreateStringLiteral(node.Name));

			Replace(mie);
		}
		
		void ProcessQuackPropertySet(BinaryExpression node)
		{
			MemberReferenceExpression target = (MemberReferenceExpression)node.Left;
			MethodInvocationExpression mie = CodeBuilder.CreateMethodInvocation(
				RuntimeServices_SetProperty,
				target.Target,
				CodeBuilder.CreateStringLiteral(target.Name),
				node.Right);
			Replace(mie);
		}
		
		void ExpandQuackInvocation(MethodInvocationExpression node)
		{
			Visit(node.Arguments);
			Visit(node.NamedArguments);

			MemberReferenceExpression target = (MemberReferenceExpression)node.Target;
			target.Target = (Expression)VisitNode(target.Target);

			node.Target = CodeBuilder.CreateMemberReference(
				CodeBuilder.CreateReference(node.LexicalInfo, _runtimeServices),
				RuntimeServices_Invoke);
			
			Expression args = CodeBuilder.CreateObjectArray(node.Arguments);
			node.Arguments.Clear();
			node.Arguments.Add(target.Target);
			node.Arguments.Add(CodeBuilder.CreateStringLiteral(target.Name));
			node.Arguments.Add(args);
		}

		bool IsDuckTyped(Expression expression)
		{
			IType type = expression.ExpressionType;
			return null != type && TypeSystemServices.IsDuckType(type);
		}

		private void BindDuck(Expression node)
		{
			BindExpressionType(node, TypeSystemServices.DuckType);
		}

		void Replace(Expression node)
		{
			BindDuck(node);
			ReplaceCurrentNode(node);
		}
	}
}
