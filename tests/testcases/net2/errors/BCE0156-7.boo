"""
BCE0156-7.boo(5,14): BCE0156: Type 'System.Array' must be an interface type or a non-final class type to be used as a type constraint on generic parameter 'T'.
"""

class C[of T(System.Array)]:
	pass

