"""
BCE0156-2.boo(8,14): BCE0156: Type 'SomeStruct' must be an interface type or a non-final class type to be used as a type constraint on generic parameter 'T'.
"""

struct SomeStruct:
	pass

class C[of T(SomeStruct)]:
	pass
