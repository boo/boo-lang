﻿#region license
// boo - an extensible programming language for the CLI
// Copyright (C) 2004 Rodrigo B. de Oliveira
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// As a special exception, if you link this library with other files to
// produce an executable, this library does not by itself cause the
// resulting executable to be covered by the GNU General Public License.
// This exception does not however invalidate any other reasons why the
// executable file might be covered by the GNU General Public License.
//
// Contact Information
//
// mailto:rbo@acm.org
#endregion

//
// DO NOT EDIT THIS FILE!
//
// This file was generated automatically by
// astgenerator.boo on 2/25/2004 1:16:55 PM
//

namespace Boo.Lang.Ast.Impl
{
	using System;
	using Boo.Lang.Ast;
	
	[Serializable]
	public abstract class ForStatementImpl : Statement
	{

		protected DeclarationCollection _declarations;
		protected Expression _iterator;
		protected Block _block;

		protected ForStatementImpl()
		{
			InitializeFields();
		}
		
		protected ForStatementImpl(LexicalInfo info) : base(info)
		{
			InitializeFields();
		}
		

		protected ForStatementImpl(Expression iterator)
		{
			InitializeFields();
			Iterator = iterator;
		}
			
		protected ForStatementImpl(LexicalInfo lexicalInfo, Expression iterator) : base(lexicalInfo)
		{
			InitializeFields();
			Iterator = iterator;
		}
			
		new public Boo.Lang.Ast.ForStatement CloneNode()
		{
			return (Boo.Lang.Ast.ForStatement)Clone();
		}

		override public NodeType NodeType
		{
			get
			{
				return NodeType.ForStatement;
			}
		}
		
		override public void Switch(IAstTransformer transformer, out Node resultingNode)
		{
			Boo.Lang.Ast.ForStatement thisNode = (Boo.Lang.Ast.ForStatement)this;
			Boo.Lang.Ast.Statement resultingTypedNode = thisNode;
			transformer.OnForStatement(thisNode, ref resultingTypedNode);
			resultingNode = resultingTypedNode;
		}

		override public bool Replace(Node existing, Node newNode)
		{
			if (base.Replace(existing, newNode))
			{
				return true;
			}

			if (_modifier == existing)
			{
				this.Modifier = (Boo.Lang.Ast.StatementModifier)newNode;
				return true;
			}

			if (_declarations != null)
			{
				Boo.Lang.Ast.Declaration item = existing as Boo.Lang.Ast.Declaration;
				if (null != item)
				{
					if (_declarations.Replace(item, (Boo.Lang.Ast.Declaration)newNode))
					{
						return true;
					}
				}
			}

			if (_iterator == existing)
			{
				this.Iterator = (Boo.Lang.Ast.Expression)newNode;
				return true;
			}

			if (_block == existing)
			{
				this.Block = (Boo.Lang.Ast.Block)newNode;
				return true;
			}

			return false;
		}

		override public object Clone()
		{
			Boo.Lang.Ast.ForStatement clone = (Boo.Lang.Ast.ForStatement)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Boo.Lang.Ast.ForStatement));
			clone._lexicalInfo = _lexicalInfo;
			clone._documentation = _documentation;
			clone._properties = (System.Collections.Hashtable)_properties.Clone();
			

			if (null != _modifier)
			{
				clone._modifier = (StatementModifier)_modifier.Clone();
			}

			if (null != _declarations)
			{
				clone._declarations = (DeclarationCollection)_declarations.Clone();
			}

			if (null != _iterator)
			{
				clone._iterator = (Expression)_iterator.Clone();
			}

			if (null != _block)
			{
				clone._block = (Block)_block.Clone();
			}
			
			return clone;
		}
			
		public DeclarationCollection Declarations
		{
			get
			{
				return _declarations;
			}
			

			set
			{
				if (_declarations != value)
				{
					_declarations = value;
					if (null != _declarations)
					{
						_declarations.InitializeParent(this);

					}
				}
			}
			

		}
		

		public Expression Iterator
		{
			get
			{
				return _iterator;
			}
			

			set
			{
				if (_iterator != value)
				{
					_iterator = value;
					if (null != _iterator)
					{
						_iterator.InitializeParent(this);

					}
				}
			}
			

		}
		

		public Block Block
		{
			get
			{
				return _block;
			}
			

			set
			{
				if (_block != value)
				{
					_block = value;
					if (null != _block)
					{
						_block.InitializeParent(this);

					}
				}
			}
			

		}
		

		private void InitializeFields()
		{
			_declarations = new DeclarationCollection(this);

			_block = new Block();
			_block.InitializeParent(this);
			

		}
	}
}
