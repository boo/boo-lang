#region license
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

namespace Boo.Lang.Compiler.TypeSystem
{
	using System;
	using System.Collections.Generic;
	using Boo.Lang.Compiler.TypeSystem;
	using Boo.Lang.Compiler.Ast;

    /// <summary>
    /// A generic method definition on an internal type.
    /// </summary>
	internal class InternalGenericMethod : InternalMethod, IGenericMethodInfo
	{
		IGenericParameter[] _genericParameters = null;
		Dictionary<IType[], IMethod> _constructedMethods = new Dictionary<IType[], IMethod>(ArrayEqualityComparer<IType>.Default);
		
		public InternalGenericMethod(TypeSystemServices tss, Method method) : base(tss, method)
		{
		}

		public override IGenericMethodInfo GenericInfo 
		{
			get { return this; }
		}
		
		public IGenericParameter[] GenericParameters
		{
			get 
			{ 
				if (_genericParameters == null)
				{
					_genericParameters = Array.ConvertAll<GenericParameterDeclaration, IGenericParameter>(
						Method.GenericParameters.ToArray(),
						delegate(GenericParameterDeclaration gpd) { return (IGenericParameter)gpd.Entity; });
				}
				return _genericParameters; 
			}
		}
		
		public IMethod ConstructMethod(IType[] arguments)
		{
			IMethod constructed = null;
			if (!_constructedMethods.TryGetValue(arguments, out constructed))
			{
				constructed = new GenericConstructedMethod(_typeSystemServices, this, arguments);
				_constructedMethods.Add(arguments, constructed);
			}
			
			return constructed;
		}
		
		public override string FullName 
		{
			get 
			{
				return string.Format("{0}`{1}", base.FullName, GenericParameters.Length);
			}
		}
		
		public override bool Resolve(List targetList, string name, EntityType flags)
		{
			// Try to resolve name as a generic parameter
			if (NameResolutionService.IsFlagSet(flags, EntityType.Type))
			{
				foreach (GenericParameterDeclaration gpd in Method.GenericParameters)
				{
					if (gpd.Name == name)
					{
						targetList.AddUnique(gpd.Entity);
						return true;
					}
				}
			}
			
			return base.Resolve(targetList, name, flags);
		}
	}	
}

