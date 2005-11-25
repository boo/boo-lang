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
	using Boo.Lang.Compiler.Ast;
	using Boo.Lang.Compiler.TypeSystem;

	public class ProcessMethodBodiesWithDuckTyping : ProcessMethodBodies
	{
		override protected void ProcessBuiltinInvocation(BuiltinFunction function, MethodInvocationExpression node)
		{
			if (TypeSystemServices.IsQuackBuiltin(function))
			{
				BindDuck(node);
			}
			else
			{
				base.ProcessBuiltinInvocation(function, node);
			}
		}
		
		override protected void ProcessAssignment(BinaryExpression node)
		{
			if (TypeSystemServices.IsQuackBuiltin(node.Left.Entity))
			{
				BindDuck(node);
			}
			else
			{
				base.ProcessAssignment(node);
			}
		}

		protected override bool ShouldRebindMember(IEntity entity)
		{
			// always rebind quack builtins (InPlace operators)
			return null == entity || TypeSystemServices.IsQuackBuiltin(entity);
		}

		
		override protected void MemberNotFound(MemberReferenceExpression node, INamespace ns)
		{
			if (TypeSystemServices.IsDuckTyped(node.Target))
			{	
				Bind(node, BuiltinFunction.Quack);
				BindDuck(node);
			}
			else
			{
				base.MemberNotFound(node, ns);
			}
		}
		
		override protected void ProcessInvocationOnUnknownCallableExpression(MethodInvocationExpression node)
		{
			if (TypeSystemServices.IsDuckTyped(node.Target))
			{
				Bind(node, BuiltinFunction.Quack);
				BindDuck(node);
			}
			else
			{
				base.ProcessInvocationOnUnknownCallableExpression(node);
			}
		}
		
		override public void LeaveSlicingExpression(SlicingExpression node)
		{
			if (TypeSystemServices.IsDuckTyped(node.Target))
			{
				BindDuck(node);
			}
			else
			{
				base.LeaveSlicingExpression(node);
			}
		}
		
		override public void LeaveUnaryExpression(UnaryExpression node)
		{
			if (TypeSystemServices.IsDuckTyped(node.Operand) &&
			   node.Operator == UnaryOperatorType.UnaryNegation)
			{
				BindDuck(node);
			}
			else
			{
				base.LeaveUnaryExpression(node);
			}
		}

		protected override bool ResolveRuntimeOperator(BinaryExpression node, string operatorName, MethodInvocationExpression mie)
		{			
			if (TypeSystemServices.IsDuckTyped(node.Left)
				|| TypeSystemServices.IsDuckTyped(node.Right))
			{
				if (AstUtil.IsOverloadableOperator(node.Operator)
					|| BinaryOperatorType.Or == node.Operator
					|| BinaryOperatorType.And == node.Operator)
				{
					BindDuck(node);
					return true;
				}
			}
			return base.ResolveRuntimeOperator(node, operatorName, mie);
		}
		
		protected override void CheckBuiltinUsage(ReferenceExpression node, IEntity entity)
		{
			if (TypeSystemServices.IsQuackBuiltin(entity)) return;
			base.CheckBuiltinUsage(node, entity);
		}

		private void BindDuck(Expression node)
		{
			BindExpressionType(node, TypeSystemServices.DuckType);
		}
	}
}
