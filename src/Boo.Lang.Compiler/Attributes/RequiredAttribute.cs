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

using System;
using Boo.Lang.Compiler.Ast;

namespace Boo.Lang
{
	/// <summary>
	/// Parameter validation.
	/// </summary>
	/// <example>
	/// <pre>
	/// def constructor([required] name as string):
	///		_name = name
	/// </pre>
	/// </example>
	//[AstAttributeTarget(typeof(ParameterDeclaration))]
	public class RequiredAttribute : Boo.Lang.Compiler.AbstractAstAttribute
	{
		protected Expression _condition;
		
		public RequiredAttribute()
		{
		}
		
		public RequiredAttribute(Expression condition)
		{
			if (null == condition)
			{
				throw new ArgumentNullException("condition");
			}
			_condition = condition;
		}

		override public void Apply(Boo.Lang.Compiler.Ast.Node node)
		{
			ParameterDeclaration pd = node as ParameterDeclaration;
			string errorMessage = null;
			
			if (null == pd)
			{
				InvalidNodeForAttribute("ParameterDeclaration");
				return;
			}

			string exceptionClass = null;
			StatementModifier modifier = null;			
			if (null == _condition)
			{
				exceptionClass = "ArgumentNullException";
				modifier = new StatementModifier(
						StatementModifierType.If,
						new BinaryExpression(BinaryOperatorType.ReferenceEquality,
							new ReferenceExpression(pd.Name),
							new NullLiteralExpression()));
			}
			else
			{
				exceptionClass = "ArgumentException";
				modifier = new StatementModifier(
						StatementModifierType.Unless,
						_condition);
				errorMessage = "Expected: " + _condition.ToString();
			}
			
			MethodInvocationExpression x = new MethodInvocationExpression();
			x.Target = new MemberReferenceExpression(
								new ReferenceExpression("System"),
								exceptionClass);
			if (null != errorMessage)
			{
				x.Arguments.Add(new StringLiteralExpression(errorMessage));
			}
			x.Arguments.Add(new StringLiteralExpression(pd.Name));
			
			RaiseStatement rs = new RaiseStatement(x, modifier);
			rs.LexicalInfo = LexicalInfo;

			Method method = pd.ParentNode as Method;
			if (null != method)
			{
				method.Body.Statements.Insert(0, rs);
			}
			else
			{
				Property property = (Property)pd.ParentNode;
				if (null != property.Getter)
				{
					property.Getter.Body.Statements.Insert(0, rs);
				}
				if (null != property.Setter)
				{
					property.Setter.Body.Statements.Insert(0, rs.CloneNode());
				}
			}
			
		}
	}
}
