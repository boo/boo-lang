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

namespace BooCompiler.Tests
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using Boo.Lang.Compiler.Ast;
	using Boo.Lang.Compiler;
	using Boo.Lang.Compiler.IO;
	using Boo.Lang.Compiler.Steps;
	using Boo.Lang.Compiler.Pipelines;
	using NUnit.Framework;
	
	public abstract class AbstractCompilerTestCase
	{
		protected BooCompiler _compiler;
		
		protected CompilerParameters _parameters;
		
		protected string _baseTestCasesPath;
		
		protected StringWriter _output;
		
		protected bool VerifyGeneratedAssemblies
		{
			get
			{
				return Boo.Lang.Compiler.Steps.PEVerify.IsSupported &&
						GetEnvironmentFlag("peverify", true);
			}
		}
		
		[TestFixtureSetUp]
		public virtual void SetUpFixture()
		{
			if (VerifyGeneratedAssemblies)
			{
				CopyDependencies();
			}
			
			_baseTestCasesPath = Path.Combine(BooTestCaseUtil.TestCasesPath, "compilation");
			
			_compiler = new BooCompiler();
			_parameters = _compiler.Parameters;			
			_parameters.OutputWriter = _output = new StringWriter();
			_parameters.OutputAssembly = Path.Combine(Path.GetTempPath(), "testcase.exe");
			_parameters.Pipeline = SetUpCompilerPipeline();
			_parameters.References.Add(typeof(NUnit.Framework.Assert).Assembly);
			_parameters.References.Add(typeof(AbstractCompilerTestCase).Assembly);
		}
		
		protected virtual void CopyDependencies()
		{
			CopyAssembly(typeof(Boo.Lang.List).Assembly);
			CopyAssembly(typeof(Boo.Lang.Compiler.BooCompiler).Assembly);
			CopyAssembly(GetType().Assembly);
			CopyAssembly(typeof(NUnit.Framework.Assert).Assembly);
			CopyAssembly(System.Reflection.Assembly.LoadWithPartialName("BooModules"));
		}
		
		public void CopyAssembly(System.Reflection.Assembly assembly)
		{
			if (null == assembly)
			{
				throw new ArgumentNullException("assembly");
			}
			string location = assembly.Location;
			File.Copy(location, Path.Combine(Path.GetTempPath(), Path.GetFileName(location)), true);			
		}
		
		[TestFixtureTearDown]
		public virtual void TearDownFixture()
		{
			Trace.Listeners.Clear();
		}
		
		[SetUp]
		public virtual void SetUpTest()
		{
			System.Threading.Thread current = System.Threading.Thread.CurrentThread;
			
			_parameters.Input.Clear();			
			
			current.CurrentCulture = current.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;			
		}		
		
		/// <summary>
		/// Override in derived classes to use a different pipeline.
		/// </summary>
		protected virtual CompilerPipeline SetUpCompilerPipeline()
		{
			CompilerPipeline pipeline = null;
			
			if (VerifyGeneratedAssemblies)
			{			
				pipeline = new CompileToFileAndVerify();				
			}
			else
			{
				pipeline = new CompileToMemory();
			}			
			
			pipeline.Add(new RunAssembly());
			return pipeline;
		}
		
		bool GetEnvironmentFlag(string name, bool defaultValue)
		{
			string value = Environment.GetEnvironmentVariable(name);
			if (null == value)
			{
				return defaultValue;
			}
			return bool.Parse(value);
		}
		
		protected void RunCompilerTestCase(string name)
		{					
			string fname = GetTestCasePath(name);
			_parameters.Input.Add(new FileInput(fname));
			RunAndAssert();
		}
		
		protected void RunMultiFileTestCase(params string[] files)
		{
			foreach (string file in files)
			{
				_parameters.Input.Add(new FileInput(GetTestCasePath(file)));
			}
			RunAndAssert();
		}
		
		protected void RunAndAssert()
		{			
			CompilerContext context;
			string output = Run(null, out context);			
			string expected = context.CompileUnit.Modules[0].Documentation;
			if (null == expected)
			{
				expected = "";
			}
			Assert.AreEqual(expected.Trim(), output.Trim(), _parameters.Input[0].Name);
		}
		
		protected string RunString(string code)
		{	
			return RunString(code, null);
		}
		
		protected string RunString(string code, string stdin)
		{
			_parameters.Input.Add(new StringInput("<teststring>", code));
			
			CompilerContext context;
			return Run(stdin, out context);
		}
		
		protected string Run(string stdin, out CompilerContext context)
		{		
			TextWriter oldStdOut = Console.Out;
			TextReader oldStdIn = Console.In;
			
			try
			{
				Console.SetOut(_output);
				if (null != stdin)
				{
					Console.SetIn(new StringReader(stdin));
				}
				
				context = _compiler.Run();
				
				if (context.Errors.Count > 0)
				{
					if (!IgnoreErrors)
					{
						Assert.Fail(GetFirstInputName(context) + ": " + context.Errors.ToString(false));
					}
				}
				return _output.ToString().Replace("\r\n", "\n");
			}
			finally
			{				
				_output.GetStringBuilder().Length = 0;
				
				Console.SetOut(oldStdOut);
				Console.SetIn(oldStdIn);
			}
		}
		
		protected virtual bool IgnoreErrors
		{
			get
			{
				return false;
			}
		}
		
		string GetFirstInputName(CompilerContext context)
		{
			return context.Parameters.Input[0].Name;
		}
		
		protected virtual string GetTestCasePath(string fname)
		{
			return Path.Combine(_baseTestCasesPath, fname);
		}
	}
}
