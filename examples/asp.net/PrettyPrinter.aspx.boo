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

namespace Boo.Examples.Web

import System
import System.IO
import System.Web
import System.Web.UI
import System.Web.UI.WebControls
import System.Web.UI.HtmlControls
import Boo.Lang.Parser
import Boo.Lang.Compiler.Ast
import Boo.Lang.Compiler.Ast.Visitors

class PrettyPrinter(BooPrinterVisitor):
	
	Server = HttpContext.Current.Server
	
	def constructor(writer as TextWriter):
		super(writer)
		
	override def Write(text as string):
		Server.HtmlEncode(text, _writer)
		
	override def WriteLine():		
		_writer.Write("<br />")
		super()
		
	override def WriteKeyword(text as string):
		_writer.Write("<span class='keyword'>${text}</span>")
		
	override def WriteOperator(text as string):
		_writer.Write("<span class='operator'>${Server.HtmlEncode(text)}</span>")
		
	override def OnExpressionInterpolationExpression(node as ExpressionInterpolationExpression):
		_writer.Write("<span class='string'>")
		super(node)	
		_writer.Write("</span>")
		
	override def WriteStringLiteral(text as string):
		_writer.Write("<span class='string'>")		
		buffer = StringWriter()		
		BooPrinterVisitor.WriteStringLiteral(text, buffer)
		Server.HtmlEncode(buffer.ToString(), _writer)
		_writer.Write("</span>")
		
	override def OnIntegerLiteralExpression(node as IntegerLiteralExpression):
		_writer.Write("<span class='integer'>${node.Value}</span>")

class PrettyPrinterPage(Page):
	
	_src as TextBox
	_pretty as HtmlContainerControl
	
	def Page_Load(sender, args as EventArgs):
		PrettyPrint() if Page.IsPostBack
		
	def PrettyPrint():		
		printer = PrettyPrinter(StringWriter(), IndentText: "&nbsp;&nbsp;")
		printer.Print(Parse())
		_pretty.InnerHtml = printer.Writer.ToString()
		
	def Parse():
		return BooParser.ParseString("<string>", _src.Text)

			
		
