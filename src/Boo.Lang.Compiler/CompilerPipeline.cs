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

namespace Boo.Lang.Compiler
{
	using System;
	using System.IO;
	using System.Collections;
	
	public class CompilerStepEventArgs : EventArgs
	{
		public CompilerContext Context;
		public ICompilerStep Step;
		
		public CompilerStepEventArgs(CompilerContext context, ICompilerStep step)
		{
			this.Context = context;
			this.Step = step;
		}
	}
	
	public delegate void CompilerStepEventHandler(object sender, CompilerStepEventArgs args);
	
	/// <summary>
	/// A ordered set of <see cref="ICompilerStep"/> implementations
	/// that should be executed in sequence.
	/// </summary>
	public class CompilerPipeline : System.MarshalByRefObject
	{	
		public event CompilerStepEventHandler BeforeStep;
		
		public event CompilerStepEventHandler AfterStep;
		
		public static CompilerPipeline GetPipeline(string name)
		{
			switch (name)
			{
				case "parse": return new Pipelines.Parse();
				case "compile": return new Pipelines.Compile();
				case "run": return new Pipelines.Run();
				case "default": return new Pipelines.CompileToFile();
				case "verify": return new Pipelines.CompileToFileAndVerify();
				case "roundtrip": return new Pipelines.ParseAndPrint();
				case "boo": return new Pipelines.CompileToBoo();
				case "xml": return new Pipelines.ParseAndPrintXml();
				case "dumpreferences":
				{
					CompilerPipeline pipeline = new Pipelines.CompileToBoo();
					pipeline.Add(new Boo.Lang.Compiler.Steps.DumpReferences());
					return pipeline;
				}
			}
			return (CompilerPipeline)Activator.CreateInstance(Type.GetType(name, true));
		}
		
		protected Boo.Lang.List _items;
		
		protected bool _breakOnErrors;

		public CompilerPipeline()
		{
			_items = new Boo.Lang.List();
			_breakOnErrors = true;
		}
		
		public bool BreakOnErrors
		{
			get
			{
				return _breakOnErrors;
			}
			
			set
			{
				_breakOnErrors = value;
			}
		}
		
		public CompilerPipeline Add(ICompilerStep step)
		{
			if (null == step)
			{
				throw new ArgumentNullException("step");
			}
			
			_items.Add(step);
			return this;
		}
		
		public CompilerPipeline RemoveAt(int index)
		{
			_items.RemoveAt(index);
			return this;
		}
		
		public CompilerPipeline Insert(int index, ICompilerStep step)
		{
			if (null == step)
			{
				throw new ArgumentNullException("step");
			}
			
			_items.Insert(index, step);
			return this;
		} 
		
		public CompilerPipeline InsertAfter(Type stepExactType, ICompilerStep step)
		{			
			return Insert(Find(stepExactType)+1, step);
		}
		
		public CompilerPipeline InsertBefore(Type stepExactType, ICompilerStep step)
		{
			return Insert(Find(stepExactType)-1, step);
		}
		
		public CompilerPipeline Replace(Type stepExactType, ICompilerStep step)
		{
			if (null == step)
			{
				throw new ArgumentNullException("step");
			}
			
			int index = Find(stepExactType);
			if (-1 == index)
			{
				throw new ArgumentException("stepExactType");
			}
			_items[index] = step;
			return this;
		}
		
		public int Find(Type stepExactType)
		{
			if (null == stepExactType)
			{
				throw new ArgumentNullException("stepExactType");
			}
			for (int i=0; i<_items.Count; ++i)
			{
				if (_items[i].GetType() == stepExactType)
				{
					return i;
				}
			}
			return -1;
		}

		public int Count
		{
			get
			{
				return _items.Count;
			}
		}

		public ICompilerStep this[int index]
		{
			get
			{
				return (ICompilerStep)_items[index];
			}
			
			set
			{
				if (null == value)
				{
					throw new ArgumentNullException("value");
				}
				_items[index] = value;
			}
		}
		
		virtual public void Clear()
		{
			_items.Clear();
		}
		
		virtual protected void OnBeforeStep(CompilerContext context, ICompilerStep step)
		{
			if (null != BeforeStep)
			{
				BeforeStep(this, new CompilerStepEventArgs(context, step));
			}
		}
		
		virtual protected void OnAfterStep(CompilerContext context, ICompilerStep step)
		{
			if (null != AfterStep)
			{
				AfterStep(this, new CompilerStepEventArgs(context, step));
			}
		}

		virtual public void Run(CompilerContext context)
		{
			foreach (ICompilerStep step in _items)
			{				
				RunStep(context, step);
				
				if (_breakOnErrors && context.Errors.Count > 0)
				{
					break;
				}
			}
			
			foreach (ICompilerStep step in _items)
			{
				step.Dispose();
			}
		}
		
		protected void RunStep(CompilerContext context, ICompilerStep step)
		{
			OnBeforeStep(context, step);	
				
			step.Initialize(context);
			try
			{
				step.Run();
			}
			catch (Boo.Lang.Compiler.CompilerError error)
			{
				context.Errors.Add(error);
			}
			catch (System.Exception x)
			{
				context.Errors.Add(CompilerErrorFactory.StepExecutionError(x, step));
			}
			finally
			{				
				OnAfterStep(context, step);
			}
		}
	}
}
