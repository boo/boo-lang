namespace MetaBoo.PipelineSteps

import MetaBoo
import MetaBoo.Ast
import MetaBoo.Ast.Visitors

class PrintBoo(AbstractCompilerPipelineStep):
	
	override def Run():
		BooPrinterVisitor(System.Console.Out).Switch(self.CompileUnit)
