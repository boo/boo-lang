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

namespace Boo.Lang
{
	using System;
	using Boo.Lang.Compiler;
	using Boo.Lang.Compiler.Ast;
	
	public abstract class AbstractPrintMacro : AbstractCompilerComponent, IAstMacro
	{
		public abstract Statement Expand(MacroStatement macro);
		
		protected Statement Expand(MacroStatement macro,
								Expression writePrototype,
								Expression writeLinePrototype)
		{	
			LexicalInfo li = macro.LexicalInfo;
			
			int argc = macro.Arguments.Count;
			if (argc < 2)
			{
				MethodInvocationExpression mie = new MethodInvocationExpression(
													li,
													writeLinePrototype.CloneNode());
				mie.Arguments = macro.Arguments;
				return new ExpressionStatement(mie);
			}
			
			Block block = new Block();
			for (int i=0; i<argc-1; ++i)
			{
				block.Add(
					AstUtil.CreateMethodInvocationExpression(
						li,
						writePrototype.CloneNode(),
						macro.Arguments[i]));
				block.Add(
					AstUtil.CreateMethodInvocationExpression(
						li,
						writePrototype.CloneNode(),
						new StringLiteralExpression(" ")));
			}
			block.Add(
				AstUtil.CreateMethodInvocationExpression(
					li,
					writeLinePrototype.CloneNode(),
					macro.Arguments[-1]));
			return block;
		}
	}
}

