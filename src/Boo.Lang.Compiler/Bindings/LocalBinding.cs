#region license
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

namespace Boo.Lang.Compiler.Infos
{
	using Boo.Lang.Compiler.Ast;
	
	public class LocalInfo : ITypedInfo
	{		
		Local _local;
		
		ITypeInfo _typeInfo;
		
		System.Reflection.Emit.LocalBuilder _builder;
		
		public LocalInfo(Local local, ITypeInfo typeInfo)
		{			
			_local = local;
			_typeInfo = typeInfo;
		}
		
		public string Name
		{
			get
			{
				return _local.Name;
			}
		}
		
		public string FullName
		{
			get
			{
				return _local.Name;
			}
		}
		
		public InfoType InfoType
		{
			get
			{
				return InfoType.Local;
			}
		}
		
		public bool IsPrivateScope
		{
			get
			{
				return _local.PrivateScope;
			}
		}
		
		public Local Local
		{
			get
			{
				return _local;
			}
		}
		
		public ITypeInfo BoundType
		{
			get
			{
				return _typeInfo;
			}
		}
		
		public System.Reflection.Emit.LocalBuilder LocalBuilder
		{
			get
			{
				return _builder;
			}
			
			set
			{
				_builder = value;
			}
		}
		
		override public string ToString()
		{
			return string.Format("Local<Name={0}, Type={1}>", Name, BoundType);
		}
	}
}
