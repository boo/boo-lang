﻿#region license
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


import Boo.Lang.Compiler
import Boo.Lang.Compiler.Ast
import Boo.Lang.Compiler.Steps
import Boo.Lang.Compiler.TypeSystem

class CustomTypeSystem(TypeSystemServices):
	def constructor(context as CompilerContext):
		super(context)
		
	override def PreparePrimitives():
		self.AddPrimitiveType("string", self.StringType)
		self.AddPrimitiveType("void", self.VoidType)
		
class InitializeCustomTypeSystem(AbstractCompilerStep):
	override def Run():
		self.Context.TypeSystemServices = CustomTypeSystem(self.Context)

pipeline = Pipelines.CompileToMemory()
pipeline.Replace(InitializeTypeSystemServices, InitializeCustomTypeSystem())
pipeline.RemoveAt(pipeline.Find(IntroduceGlobalNamespaces))

code = """
import System.Console
WriteLine(date.Now)
WriteLine(List())
"""

compiler = BooCompiler()
compiler.Parameters.Input.Add(IO.StringInput("code.boo", code))
compiler.Parameters.Pipeline = pipeline
compiler.Parameters.OutputType = CompilerOutputType.Library

result = compiler.Run()
print result.Errors.ToString(true)
