import System
import NUnit.Framework

class LooksLikeADuck:
	def Quack():
		return "quack!"

class QuacksLikeADuck:
	def Quack():
		return "quack!"
		
class NotExactlyADuck:
	def Bark():
		return "au!"

def quack(obj as duck):
	Assert.AreEqual("quack!", obj.Quack())	
	
quack(LooksLikeADuck())
quack(QuacksLikeADuck())

try:
	quack(NotExactlyADuck())
	Assert.Fail("Expected MissingMethodException!")
except x as MissingMethodException:
	pass
	

