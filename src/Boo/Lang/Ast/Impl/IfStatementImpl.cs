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
// astgenerator.boo on 3/26/2004 11:12:00 AM
//

namespace Boo.Lang.Ast.Impl
{	
	using Boo.Lang.Ast;
	using System.Collections;
	using System.Runtime.Serialization;
	
	[System.Serializable]
	public abstract class IfStatementImpl : Statement
	{

		protected Expression _condition;
		protected Block _trueBlock;
		protected Block _falseBlock;

		protected IfStatementImpl()
		{
			InitializeFields();
		}
		
		protected IfStatementImpl(LexicalInfo info) : base(info)
		{
			InitializeFields();
		}
		

		protected IfStatementImpl(Expression condition, Block trueBlock, Block falseBlock)
		{
			InitializeFields();
			Condition = condition;
			TrueBlock = trueBlock;
			FalseBlock = falseBlock;
		}
			
		protected IfStatementImpl(LexicalInfo lexicalInfo, Expression condition, Block trueBlock, Block falseBlock) : base(lexicalInfo)
		{
			InitializeFields();
			Condition = condition;
			TrueBlock = trueBlock;
			FalseBlock = falseBlock;
		}
			
		new public IfStatement CloneNode()
		{
			return Clone() as IfStatement;
		}

		override public NodeType NodeType
		{
			get
			{
				return NodeType.IfStatement;
			}
		}
		
		override public void Switch(IAstTransformer transformer, out Node resultingNode)
		{
			IfStatement thisNode = (IfStatement)this;
			Statement resultingTypedNode = thisNode;
			transformer.OnIfStatement(thisNode, ref resultingTypedNode);
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
				this.Modifier = (StatementModifier)newNode;
				return true;
			}

			if (_condition == existing)
			{
				this.Condition = (Expression)newNode;
				return true;
			}

			if (_trueBlock == existing)
			{
				this.TrueBlock = (Block)newNode;
				return true;
			}

			if (_falseBlock == existing)
			{
				this.FalseBlock = (Block)newNode;
				return true;
			}

			return false;
		}

		override public object Clone()
		{
			IfStatement clone = FormatterServices.GetUninitializedObject(typeof(IfStatement)) as IfStatement;
			clone._lexicalInfo = _lexicalInfo;
			clone._documentation = _documentation;
			clone._properties = _properties.Clone() as Hashtable;
			

			if (null != _modifier)
			{
				clone._modifier = _modifier.Clone() as StatementModifier;
				clone._modifier.InitializeParent(clone);
			}

			if (null != _condition)
			{
				clone._condition = _condition.Clone() as Expression;
				clone._condition.InitializeParent(clone);
			}

			if (null != _trueBlock)
			{
				clone._trueBlock = _trueBlock.Clone() as Block;
				clone._trueBlock.InitializeParent(clone);
			}

			if (null != _falseBlock)
			{
				clone._falseBlock = _falseBlock.Clone() as Block;
				clone._falseBlock.InitializeParent(clone);
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
		

		public Block TrueBlock
		{
			get
			{
				return _trueBlock;
			}
			

			set
			{
				if (_trueBlock != value)
				{
					_trueBlock = value;
					if (null != _trueBlock)
					{
						_trueBlock.InitializeParent(this);

					}
				}
			}
			

		}
		

		public Block FalseBlock
		{
			get
			{
				return _falseBlock;
			}
			

			set
			{
				if (_falseBlock != value)
				{
					_falseBlock = value;
					if (null != _falseBlock)
					{
						_falseBlock.InitializeParent(this);

					}
				}
			}
			

		}
		

		private void InitializeFields()
		{

		}
	}
}
