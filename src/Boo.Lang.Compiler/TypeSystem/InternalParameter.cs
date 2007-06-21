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

using Boo.Lang.Compiler.Ast;

namespace Boo.Lang.Compiler.TypeSystem
{	
	public class InternalParameter : AbstractLocalEntity, IParameter, ILocalEntity, IInternalEntity
	{
		ParameterDeclaration _parameter;
		
		int _index;
		
		public InternalParameter(ParameterDeclaration parameter, int index)
		{
			_parameter = parameter;
			_index = index;
		}

		public Node Node
		{
			get { return _parameter;  }
		}
		
		public string Name
		{
			get { return _parameter.Name; }
		}
		
		public string FullName
		{
			get { return _parameter.Name; }
		}
		
		public EntityType EntityType
		{
			get { return EntityType.Parameter; }
		}
		
		public ParameterDeclaration Parameter
		{
			get { return _parameter; }
		}
		
		public IType Type
		{
			get { return (IType)TypeSystemServices.GetEntity(_parameter.Type); }
		}
		
		public int Index
		{
			get { return _index; }
			
			set { _index = value; }
		}
		
		public bool IsPrivateScope
		{
			get { return false; }
		}

		public bool IsDuckTyped
		{
			get { return false; }
		}
		
		public bool IsByRef
		{
			get { return _parameter.IsByRef; }
		}
	}
}
