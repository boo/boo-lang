"""
compile time
runtime
before
during
after
"""
import Boo.Lang.Compiler.Ast
import Boo.Lang.Compiler.MetaProgramming

[meta]
def surround(block as BlockExpression):	
	print "compile time"
	return [|
		print("before")
		$(block.Body)
		print("after")
	|]

typeDef = [|
	class Test:
		def Run():
			surround:
				print "during"
|]

try:
	type = compile(typeDef, System.Reflection.Assembly.GetExecutingAssembly())
except x as CompilationErrorsException:
	print x.Errors.ToString(true)
print "runtime"
(type() as duck).Run()

