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
// astgenerator.boo on 3/3/2004 11:15:05 AM
//

namespace Boo.Lang.Ast.Impl
{
	using System;
	using Boo.Lang.Ast;
	
	[Serializable]
	public abstract class WhileStatementImpl : Statement
	{

		protected Expression _condition;
		protected Block _block;

		protected WhileStatementImpl()
		{
			InitializeFields();
		}
		
		protected WhileStatementImpl(LexicalInfo info) : base(info)
		{
			InitializeFields();
		}
		

		protected WhileStatementImpl(Expression condition)
		{
			InitializeFields();
			Condition = condition;
		}
			
		protected WhileStatementImpl(LexicalInfo lexicalInfo, Expression condition) : base(lexicalInfo)
		{
			InitializeFields();
			Condition = condition;
		}
			
		new public Boo.Lang.Ast.WhileStatement CloneNode()
		{
			return Clone() as Boo.Lang.Ast.WhileStatement;
		}

		override public NodeType NodeType
		{
			get
			{
				return NodeType.WhileStatement;
			}
		}
		
		override public void Switch(IAstTransformer transformer, out Node resultingNode)
		{
			Boo.Lang.Ast.WhileStatement thisNode = (Boo.Lang.Ast.WhileStatement)this;
			Boo.Lang.Ast.Statement resultingTypedNode = thisNode;
			transformer.OnWhileStatement(thisNode, ref resultingTypedNode);
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
				this.Modifier = ((Boo.Lang.Ast.StatementModifier)newNode);
				return true;
			}

			if (_condition == existing)
			{
				this.Condition = ((Boo.Lang.Ast.Expression)newNode);
				return true;
			}

			if (_block == existing)
			{
				this.Block = ((Boo.Lang.Ast.Block)newNode);
				return true;
			}

			return false;
		}

		override public object Clone()
		{
			Boo.Lang.Ast.WhileStatement clone = (Boo.Lang.Ast.WhileStatement)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Boo.Lang.Ast.WhileStatement));
			clone._lexicalInfo = _lexicalInfo;
			clone._documentation = _documentation;
			clone._properties = (System.Collections.Hashtable)_properties.Clone();
			

			if (null != _modifier)
			{
				clone._modifier = ((StatementModifier)_modifier.Clone());
				clone._modifier.InitializeParent(clone);
			}

			if (null != _condition)
			{
				clone._condition = ((Expression)_condition.Clone());
				clone._condition.InitializeParent(clone);
			}

			if (null != _block)
			{
				clone._block = ((Block)_block.Clone());
				clone._block.InitializeParent(clone);
			}
			
			return clone;
		}
			
		public Expression Condition
		{
			get
			{
				return _condition;
			}
			

			set
			{
				if (_condition != value)
				{
					_condition = value;
					if (null != _condition)
					{
						_condition.InitializeParent(this);

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

			_block = new Block();
			_block.InitializeParent(this);
			

		}
	}
}
