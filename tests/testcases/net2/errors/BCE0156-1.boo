"""
BCE0156-1.boo(8,14): BCE0156: Type 'SomeClass' must be an interface type or a non-final class type to be used as a type constraint on generic parameter 'T'.
"""

final class SomeClass:
	pass

class C[of T(SomeClass)]:
	pass

