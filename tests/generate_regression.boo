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

import System
import System.IO

def MapPath(path):
	return Path.Combine(Project.BaseDirectory, path) 

def GetTestCaseName(fname as string):
	return Path.GetFileNameWithoutExtension(fname).Replace("-", "_")
	
def WriteTestCases(writer as TextWriter, baseDir as string):
	count = 0
	for fname in Directory.GetFiles(MapPath(baseDir)):
		continue unless fname.EndsWith(".boo")
		++count		
		writer.Write("""
		[Test]
		public void ${GetTestCaseName(fname)}()
		{
			RunCompilerTestCase(@"${Path.GetFullPath(fname)}");
		}
		""")
	print("${count} test cases found in ${baseDir}.")

def GenerateTestFixture(srcDir as string, targetFile as string, header as string):
	using writer=StreamWriter(MapPath(targetFile)):
		writer.Write(header)	
		WriteTestCases(writer, srcDir)
		writer.Write("""
	}
}
""")

GenerateTestFixture("testcases/regression", "build/RegressionTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;
	
	[TestFixture]		
	public class RegressionTestFixture : AbstractCompilerTestCase
	{
""")

GenerateTestFixture("testcases/errors", "build/CompilerErrorsTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;
	using Boo.Lang.Compiler;	

	[TestFixture]
	public class CompilerErrorsTestFixture : AbstractCompilerTestCase
	{			
		public class PrintErrors : Boo.Lang.Compiler.Pipelines.Compile
		{
			override public void Run(CompilerContext context)
			{
				base.Run(context);
				RunStep(context, new Boo.Lang.Compiler.Steps.PrintErrors());
			}
		}
		
		protected override CompilerPipeline SetUpCompilerPipeline()
		{
			return new PrintErrors();
		}
		
		protected override bool IgnoreErrors
		{
			get
			{
				return true;
			}
		}
""")

GenerateTestFixture("testcases/warnings", "build/CompilerWarningsTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;
	using Boo.Lang.Compiler;	

	[TestFixture]
	public class CompilerWarningsTestFixture : AbstractCompilerTestCase
	{	
		protected override CompilerPipeline SetUpCompilerPipeline()
		{
			CompilerPipeline pipeline = new Boo.Lang.Compiler.Pipelines.Compile();
			pipeline.Add(new Boo.Lang.Compiler.Steps.PrintWarnings());
			return pipeline;
		}
""")

GenerateTestFixture("testcases/integration", "build/IntegrationTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;

	[TestFixture]
	public class IntegrationTestFixture : AbstractCompilerTestCase
	{
""")

GenerateTestFixture("testcases/macros", "build/MacrosTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;

	[TestFixture]
	public class MacrosTestFixture : AbstractCompilerTestCase
	{
""")

GenerateTestFixture("testcases/stdlib", "build/StdlibTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;

	[TestFixture]
	public class StdlibTestFixture : AbstractCompilerTestCase
	{
""")

GenerateTestFixture("testcases/attributes", "build/AttributesTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;
	using Boo.Lang.Compiler;
	using Boo.Lang.Compiler.Steps;
	
	[TestFixture]
	public class AttributesTestFixture : AbstractCompilerTestCase
	{
		override protected CompilerPipeline SetUpCompilerPipeline()
		{
			CompilerPipeline pipeline = new Boo.Lang.Compiler.Pipelines.Parse();
			pipeline.Add(new InitializeTypeSystemServices());
			pipeline.Add(new InitializeNameResolutionService());
			pipeline.Add(new IntroduceGlobalNamespaces());	
			pipeline.Add(new BindNamespaces());
			pipeline.Add(new BindAndApplyAttributes());
			pipeline.Add(new PrintBoo());
			return pipeline;
		}
""")

GenerateTestFixture("testcases/parser/roundtrip", "build/ParserRoundtripTestFixture.cs", """
namespace Boo.Lang.Parser.Tests
{
	using NUnit.Framework;
	
	[TestFixture]
	public class ParserRoundtripTestFixture : AbstractParserTestFixture
	{
		void RunCompilerTestCase(string fname)
		{
			RunParserTestCase(fname);
		}
""")

GenerateTestFixture("testcases/semantics", "build/SemanticsTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;
	using Boo.Lang.Compiler;
	using Boo.Lang.Compiler.Pipelines;
	
	[TestFixture]
	public class SemanticsTestFixture : AbstractCompilerTestCase
	{
		protected override CompilerPipeline SetUpCompilerPipeline()
		{
			return new CompileToBoo();
		}
""")

GenerateTestFixture("testcases/ducky", "build/DuckyTestFixture.cs", """
namespace BooCompiler.Tests
{
	using NUnit.Framework;
	using Boo.Lang.Compiler;
	using Boo.Lang.Compiler.Pipelines;
	
	[TestFixture]
	public class DuckyTestFixture : AbstractCompilerTestCase
	{
		protected override void CustomizeCompilerParameters()
		{
			_parameters.Ducky = true;
		}
""")


	
